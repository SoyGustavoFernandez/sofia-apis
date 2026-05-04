using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Infrastructure.Authentication;
using SOFIA.Infrastructure.Persistence;

namespace SOFIA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddDbContext<ApplicationDbContext>(options =>
            _ = options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        _ = services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        // Autenticación
        _ = services.AddScoped<IPasswordHasher, PasswordHasher>();
        _ = services.AddScoped<IJwtProvider, JwtProvider>();
        _ = services.Configure<JwtOptions>(configuration.GetSection("Jwt"));

        var jwtOptions = configuration.GetSection("Jwt").Get<JwtOptions>()
            ?? throw new InvalidOperationException("JWT configuration is missing.");

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
                        var userIdClaim = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
                        var securityStampClaim = context.Principal?.FindFirstValue("securityStamp");

                        if (string.IsNullOrEmpty(userIdClaim) || string.IsNullOrEmpty(securityStampClaim) || !Guid.TryParse(userIdClaim, out var userId))
                        {
                            context.Fail("Unauthorized");
                            return;
                        }

                        var securityStampValid = await dbContext.Cuentas
                            .AnyAsync(c => c.Id == userId &&
                                           c.SecurityStamp.ToString() == securityStampClaim &&
                                           c.CuentaActiva &&
                                           !c.IsDeleted);

                        if (!securityStampValid)
                        {
                            context.Fail("Unauthorized");
                        }
                    }
                };
            });

        _ = services.AddAuthorization();

        return services;
    }
}
