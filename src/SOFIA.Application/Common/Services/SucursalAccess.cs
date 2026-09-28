using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Common.Services;

public sealed class SucursalAccess(IApplicationDbContext dbContext, ICurrentUser currentUser) : ISucursalAccess
{
    private IReadOnlySet<Guid>? _allowed;
    private bool _resolved;

    public async Task<IReadOnlySet<Guid>?> GetAllowedSucursalesAsync(CancellationToken cancellationToken)
    {
        if (_resolved)
        {
            return _allowed;
        }

        _allowed = currentUser.IsInRole(Rol.AdminRoleName) ? null : await ResolveAsync(cancellationToken);
        _resolved = true;
        return _allowed;
    }

    public async Task<bool> CanAccessAsync(Guid sucursalId, CancellationToken cancellationToken)
    {
        var allowed = await GetAllowedSucursalesAsync(cancellationToken);
        return allowed is null || allowed.Contains(sucursalId);
    }

    private async Task<IReadOnlySet<Guid>> ResolveAsync(CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated)
        {
            return new HashSet<Guid>();
        }

        var baseId = Guid.TryParse(currentUser.SucursalId, out var parsedBase) ? parsedBase : Guid.Empty;
        var cuentaId = Guid.TryParse(currentUser.CuentaId, out var parsedCuenta) ? parsedCuenta : Guid.Empty;

        // Account links are read through the Cuenta navigations: CuentaSucursal is a shared-type join entity
        var cuenta = dbContext.Cuentas.Where(c => c.Id == cuentaId);
        var cuentaSucursales = cuenta.SelectMany(c => c.Sucursales).Select(s => s.Id);
        var rolIds = cuenta.SelectMany(c => c.Roles).Select(r => r.Id);
        var rolSucursales = dbContext.RolesSucursales
            .Where(rs => rolIds.Contains(rs.RolId))
            .Select(rs => rs.SucursalId);

        // Intersect with the tenant-filtered branches so stale or foreign assignment rows never grant access
        var ids = await dbContext.Sucursales
            .AsNoTracking()
            .Where(s => s.Id == baseId || cuentaSucursales.Contains(s.Id) || rolSucursales.Contains(s.Id))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        return ids.ToHashSet();
    }
}
