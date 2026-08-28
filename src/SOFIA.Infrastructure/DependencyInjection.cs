using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Infrastructure.Authentication;
using SOFIA.Infrastructure.Persistence;
using SOFIA.Infrastructure.Services;

namespace SOFIA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddDbContext<ApplicationDbContext>(options =>
        {
            _ = options.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));
            _ = options.ConfigureWarnings(w => w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.CoreEventId.PossibleIncorrectRequiredNavigationWithQueryFilterInteractionWarning));
        });

        _ = services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Authentication
        _ = services.AddScoped<IPasswordHasher, PasswordHasher>();
        _ = services.AddScoped<IJwtProvider, JwtProvider>();
        _ = services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

        if (string.IsNullOrEmpty(jwtOptions.SecretKey))
            throw new InvalidOperationException("CRITICAL: JWT SecretKey is not configured. Provide Jwt:SecretKey via environment secrets.");

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
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SecretKey))
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

                        var securityStampValid = await dbContext.Cuentas
                            .AnyAsync(c => c.Id == userId &&
                                           c.SecurityStamp == securityStamp &&
                                           c.CuentaActiva &&
                                           !c.IsDeleted);

                        if (!securityStampValid)
                        {
                            context.Fail("Unauthorized: Security stamp is invalid or account is inactive.");
                        }
                    }
                };
            });

        _ = services.AddMemoryCache();
        _ = services.AddSingleton<IAuthorizationPolicyProvider, PermissionPolicyProvider>();
        _ = services.AddScoped<IAuthorizationHandler, PermissionAuthorizationHandler>();
        _ = services.AddHostedService<OutboxProcessor>();

        // AI and Privacy Services
        _ = services.AddHttpClient<IPrivacyService, PresidioPrivacyService>(client =>
        {
            var presidioUrl = configuration["PresidioApi:BaseUrl"] ?? throw new InvalidOperationException("CRITICAL: La URL de Presidio (PresidioApi:BaseUrl) no está configurada.");
            client.BaseAddress = new Uri(presidioUrl);
        });
        _ = services.AddHttpClient<IRecetaAnalyzer, GeminiRecetaAnalyzer>();
        _ = services.AddScoped<IBuscadorService, BuscadorFuzzyService>();

        _ = services.AddAuthorization();

        return services;
    }
}
