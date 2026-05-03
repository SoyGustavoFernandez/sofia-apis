using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Infrastructure.Persistence;

namespace SOFIA.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        _ = services.AddDbContext<ApplicationDbContext>(options =>
            _ = options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

        _ = services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        return services;
    }
}
