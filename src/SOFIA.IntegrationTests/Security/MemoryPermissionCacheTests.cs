using Microsoft.Extensions.Caching.Memory;
using SOFIA.Infrastructure.Authentication;

namespace SOFIA.IntegrationTests.Security;

public class MemoryPermissionCacheTests
{
    private static readonly HashSet<(string Modulo, string Accion)> Permisos = [("Ventas", "Crear")];

    private readonly MemoryPermissionCache _cache = new(new MemoryCache(new MemoryCacheOptions()));

    [Fact]
    public void TryGet_AfterSet_ReturnsCachedPermissionsIgnoringRoleNameCase()
    {
        var empresaId = Guid.NewGuid();
        _cache.Set(empresaId, "Cajero", Permisos);

        var found = _cache.TryGet(empresaId, "CAJERO", out var permissions);

        _ = found.Should().BeTrue();
        _ = permissions.Should().BeEquivalentTo(Permisos);
    }

    [Fact]
    public void TryGet_OtherEmpresaWithSameRoleName_ReturnsFalse()
    {
        _cache.Set(Guid.NewGuid(), "Cajero", Permisos);

        _ = _cache.TryGet(Guid.NewGuid(), "Cajero", out _).Should().BeFalse();
    }

    [Fact]
    public void Invalidate_RemovesOnlyTheGivenEmpresaAndRole()
    {
        var empresaId = Guid.NewGuid();
        var otraEmpresaId = Guid.NewGuid();
        _cache.Set(empresaId, "Cajero", Permisos);
        _cache.Set(empresaId, "Supervisor", Permisos);
        _cache.Set(otraEmpresaId, "Cajero", Permisos);

        _cache.Invalidate(empresaId, "Cajero");

        _ = _cache.TryGet(empresaId, "Cajero", out _).Should().BeFalse();
        _ = _cache.TryGet(empresaId, "Supervisor", out _).Should().BeTrue();
        _ = _cache.TryGet(otraEmpresaId, "Cajero", out _).Should().BeTrue();
    }
}
