using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Dashboard.Common;

public static class DashboardScopeResolver
{
    // A null allowed set (Admin) becomes every branch of the tenant, so queries always filter by explicit branch ids
    public static async Task<DashboardScope> ResolveAsync(IApplicationDbContext context, ISucursalAccess sucursalAccess, CancellationToken cancellationToken)
    {
        var allowed = await sucursalAccess.GetAllowedSucursalesAsync(cancellationToken);

        var sucursales = await context.Sucursales
            .AsNoTracking()
            .WhereSucursalIn(s => s.Id, allowed)
            .OrderBy(s => s.Nombre)
            .Select(s => new DashboardSucursal(s.Id, s.Nombre))
            .ToListAsync(cancellationToken);

        return new DashboardScope(sucursales, allowed is null);
    }
}
