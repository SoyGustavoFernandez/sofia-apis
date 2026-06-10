using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SOFIA.Infrastructure.Persistence;

namespace SOFIA.IntegrationTests.Infrastructure;

/// <summary>
/// Clase base para todos los integration tests.
/// Cada test crea su propio scope para evitar conflictos de DbContext concurrente.
/// </summary>
public abstract class BaseIntegrationTest : IClassFixture<SofiaWebAppFactory>, IDisposable
{
    protected readonly SofiaWebAppFactory Factory;
    protected readonly ISender Sender;
    protected readonly ApplicationDbContext DbContext;
    private readonly IServiceScope _scope;

    protected BaseIntegrationTest(SofiaWebAppFactory factory)
    {
        Factory = factory;
        // Scope fresco por cada instancia de test — evita DbContext compartido entre tests paralelos
        _scope = factory.Services.CreateScope();

        Sender = _scope.ServiceProvider.GetRequiredService<ISender>();
        DbContext = _scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    }

    public void Dispose() => _scope.Dispose();
}
