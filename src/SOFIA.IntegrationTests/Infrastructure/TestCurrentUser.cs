using SOFIA.Application.Common.Interfaces;

namespace SOFIA.IntegrationTests.Infrastructure;

/// <summary>
/// Replaces the HTTP-based current user so tests run inside a known tenant and can switch it.
/// </summary>
public sealed class TestCurrentUser : ICurrentUser
{
    public static readonly Guid DefaultEmpresaId = Guid.Parse("6f1b2c3d-0000-4000-8000-000000000001");

    public Guid? Empresa { get; set; } = DefaultEmpresaId;

    public string? Id => null;
    public string? Name => "integration-test";
    public string? SucursalId => null;
    public string? EmpresaId => Empresa?.ToString();
    public bool IsAuthenticated => Empresa is not null;
    public string? ClientIpAddress => "127.0.0.1";
}
