using System.Reflection;
using FluentValidation;
using SOFIA.Application.Common.Behaviors;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Services;

namespace SOFIA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            _ = cfg.AddOpenBehavior(typeof(SanitizationBehavior<,>));
            _ = cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
            _ = cfg.AddOpenBehavior(typeof(TransactionBehavior<,>));
            _ = cfg.AddOpenBehavior(typeof(AuditBehavior<,>));
        });

        _ = services.AddScoped<ISucursalAccess, SucursalAccess>();

        _ = services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
