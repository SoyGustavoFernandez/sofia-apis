using System.Reflection;
using FluentValidation;
using SOFIA.Application.Common.Behaviors;

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
        });

        _ = services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        return services;
    }
}
