using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Azure.Communication.Email;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security;
using SOFIA.Infrastructure.Authentication;
using SOFIA.Infrastructure.Email;
using SOFIA.Infrastructure.Excel;
using SOFIA.Infrastructure.Persistence;
using SOFIA.Infrastructure.Services;

namespace SOFIA.Infrastructure;

public static class DependencyInjection
{
    private static readonly TimeSpan EmpresaVigenciaCacheTtl = TimeSpan.FromMinutes(1);

    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        _ = services.AddDbContext<ApplicationDbContext>(options =>
        {
            _ = options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            _ = options.ConfigureWarnings(w => w
                .Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning)
                // Failed saves carry SQL messages with key values (e.g. DNIs); GlobalExceptionHandler logs them sanitized instead
                .Log(
                    (Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.SaveChangesFailed, Microsoft.Extensions.Logging.LogLevel.Debug),
                    (Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.CommandError, Microsoft.Extensions.Logging.LogLevel.Debug)));
        });

        _ = services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Excel
        _ = services.AddScoped<IExcelReaderService, ExcelReaderService>();

        // Sanitization
        _ = services.AddSingleton<ISanitizer, HtmlSanitizerService>();

        // Authentication
        _ = services.AddScoped<IPasswordHasher, PasswordHasher>();
        _ = services.AddScoped<IJwtProvider, JwtProvider>();
        _ = services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        jwtOptions.EnsureValid();

        _ = services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey)),
                    ValidAlgorithms = [SecurityAlgorithms.HmacSha256],
                    ClockSkew = TimeSpan.FromSeconds(30),
                };

                options.Events = new JwtBearerEvents
                {
                    OnTokenValidated = async context =>
                    {
                        var dbContext = context.HttpContext.RequestServices.GetRequiredService<IApplicationDbContext>();

                        var userIdClaim = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                                         ?? context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);

                        var securityStampClaim = context.Principal?.FindFirstValue("securityStamp");

                        if (string.IsNullOrEmpty(userIdClaim) ||
                            string.IsNullOrEmpty(securityStampClaim) ||
                            !Guid.TryParse(userIdClaim, out var userId) ||
                            !Guid.TryParse(securityStampClaim, out var securityStamp))
                        {
                            context.Fail("Unauthorized: Missing or invalid security claims.");
                            return;
                        }

                        // HttpContext.User is not populated yet at this point, so the tenant filter cannot apply
                        var requiereCambioClave = await dbContext.Cuentas
                            .IgnoreQueryFilters([QueryFilters.Tenant])
                            .Where(c => c.Id == userId &&
                                        c.SecurityStamp == securityStamp &&
                                        c.CuentaActiva &&
                                        !c.IsDeleted)
                            .Select(c => (bool?)c.RequiereCambioClave)
                            .FirstOrDefaultAsync();

                        if (requiereCambioClave is null)
                        {
                            context.Fail("Unauthorized: Security stamp is invalid or account is inactive.");
                            return;
                        }

                        SyncPasswordChangeClaim(context.Principal, requiereCambioClave.Value);

                        if (!Guid.TryParse(context.Principal?.FindFirstValue("empresaId"), out var empresaId))
                        {
                            context.Fail("Unauthorized: Missing or invalid company claim.");
                            return;
                        }

                        // Cached per company so suspending or expiring it cuts live access tokens within this TTL
                        var cache = context.HttpContext.RequestServices.GetRequiredService<IMemoryCache>();
                        var empresaVigente = await cache.GetOrCreateAsync($"empresa-vigente:{empresaId}", entry =>
                        {
                            entry.AbsoluteExpirationRelativeToNow = EmpresaVigenciaCacheTtl;
                            return dbContext.EmpresaEstaVigenteAsync(empresaId, context.HttpContext.RequestAborted);
                        });

                        if (!empresaVigente)
                        {
                            context.Fail("Unauthorized: The company's subscription is not active.");
                        }
                    }
                };
            });

        _ = services.AddMemoryCache();
        _ = services.AddSingleton<IPermissionCache, MemoryPermissionCache>();
        _ = services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        _ = services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        _ = services.AddHostedService<OutboxProcessor>();

        // AI and Privacy Services
        _ = services.AddHttpClient<IPrivacyService, PresidioPrivacyService>(client => ConfigurePresidioClient(client, configuration));
        _ = services.AddHttpClient<IRecetaAnalyzer, GeminiRecetaAnalyzer>();
        _ = services.AddScoped<IBuscadorService, BuscadorFuzzyService>();

        AddEmail(services, configuration, environment);

        // Deny by default: endpoints without auth metadata require an authenticated user
        _ = services.AddAuthorization(options => options.FallbackPolicy = new AuthorizationPolicyBuilder()
            .RequireAuthenticatedUser()
            .Build());

        return services;
    }

    // Fails fast on incomplete settings; only Development/Testing may run without a provider (recovery emails are then discarded)
    private static void AddEmail(IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
    {
        var isDevelopment = environment.IsDevelopment() || environment.IsEnvironment("Testing");
        var emailOptions = configuration.GetSection(EmailOptions.SectionName).Get<EmailOptions>() ?? new EmailOptions();
        emailOptions.EnsureValid(allowDisabled: isDevelopment);

        var appOptions = configuration.GetSection(AppOptions.SectionName).Get<AppOptions>() ?? new AppOptions();
        if (!emailOptions.IsDisabled || !isDevelopment)
        {
            appOptions.EnsureValid(allowHttp: isDevelopment);
        }

        _ = services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        _ = services.Configure<AppOptions>(configuration.GetSection(AppOptions.SectionName));
        _ = services.AddSingleton<IFrontendLinks, FrontendLinks>();

        if (emailOptions.IsDisabled)
        {
            _ = services.AddSingleton<IEmailSender, DisabledEmailSender>();
        }
        else if (emailOptions.Provider.Equals(EmailOptions.ProviderSmtp, StringComparison.OrdinalIgnoreCase))
        {
            _ = services.AddSingleton<IEmailSender, SmtpEmailSender>();
        }
        else
        {
            _ = services.AddSingleton(new EmailClient(emailOptions.AzureCommunication.ConnectionString));
            _ = services.AddSingleton<IEmailSender, AzureCommunicationEmailSender>();
        }
    }

    private static void ConfigurePresidioClient(HttpClient client, IConfiguration configuration)
    {
        var presidioUrl = configuration["PresidioApi:BaseUrl"] ?? throw new InvalidOperationException("CRITICAL: Presidio URL (PresidioApi:BaseUrl) is not configured.");
        client.BaseAddress = new Uri(presidioUrl);

        // Optional shared secret checked by Presidio when PRESIDIO_INTERNAL_KEY is set on its side
        var presidioKey = configuration["PresidioApi:InternalKey"];
        if (!string.IsNullOrWhiteSpace(presidioKey))
        {
            client.DefaultRequestHeaders.Add("X-Internal-Key", presidioKey);
        }
    }

    // The database flag wins over the token, so a change forced by an admin also applies to live access tokens
    private static void SyncPasswordChangeClaim(ClaimsPrincipal? principal, bool requiereCambioClave)
    {
        if (principal?.Identity is not ClaimsIdentity identity)
        {
            return;
        }

        foreach (var claim in identity.FindAll(PasswordChangePolicy.ClaimType).ToList())
        {
            identity.RemoveClaim(claim);
        }

        if (requiereCambioClave)
        {
            identity.AddClaim(new Claim(PasswordChangePolicy.ClaimType, "true"));
        }
    }
}
