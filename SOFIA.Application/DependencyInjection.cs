
using System.Reflection;

namespace SOFIA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        _ = services.AddMediatR(cfg =>
        {
            _ = cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
        });

        // Registro de otros servicios de aplicación (Mappers, Validators, etc.)

        return services;
    }
}
