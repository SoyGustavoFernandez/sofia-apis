using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SOFIA.Infrastructure.Persistence;
using Testcontainers.MsSql;

namespace SOFIA.IntegrationTests.Infrastructure;

/// <summary>
/// WebApplicationFactory que levanta un SQL Server real en Docker (Testcontainers).
/// Requiere Docker Desktop corriendo.
/// Password leído desde user-secrets (desarrollo) o variable de entorno SOFIA_TEST_DB_PASSWORD (CI/CD).
/// Configurar con: dotnet user-secrets set "Testing:ContainerDbPassword" "TuPassword123!" --project src/SOFIA.IntegrationTests
/// </summary>
public class SofiaWebAppFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private static readonly string _testDbPassword = ResolveTestDbPassword();

    private readonly MsSqlContainer _dbContainer = new MsSqlBuilder()
        .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
        .WithPassword(_testDbPassword)
        .Build();

    private static string ResolveTestDbPassword()
    {
        // 1. Env var (CI/CD)
        var fromEnv = Environment.GetEnvironmentVariable("SOFIA_TEST_DB_PASSWORD");
        if (!string.IsNullOrEmpty(fromEnv))
        {
            return fromEnv;
        }

        // 2. User-secrets (desarrollo local)
        var config = new ConfigurationBuilder()
            .AddUserSecrets<SofiaWebAppFactory>()
            .Build();

        var fromSecrets = config["Testing:ContainerDbPassword"];
        return !string.IsNullOrEmpty(fromSecrets)
            ? fromSecrets
            : throw new InvalidOperationException(
            "No se encontró la contraseña para el contenedor de tests. " +
            "Configúrala con: dotnet user-secrets set \"Testing:ContainerDbPassword\" \"TuPassword123!\" --project src/SOFIA.IntegrationTests");
    }

    public async Task InitializeAsync() => await _dbContainer.StartAsync();

    public new async Task DisposeAsync() => await _dbContainer.DisposeAsync();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        _ = builder.UseEnvironment("Development");

        _ = builder.ConfigureServices(services =>
        {
            // Reemplaza el DbContext registrado con uno apuntando al contenedor de test
            var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
            if (descriptor is not null)
            {
                _ = services.Remove(descriptor);
            }

            _ = services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(_dbContainer.GetConnectionString()));

            // Ejecuta las migraciones/schema contra la BD de test
            using var scope = services.BuildServiceProvider().CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            _ = db.Database.EnsureCreated();
        });
    }

    /// <summary>Crea un scope para acceder a servicios dentro de los tests.</summary>
    public IServiceScope CreateTestScope() => Services.CreateScope();
}
