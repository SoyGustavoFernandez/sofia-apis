using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Dashboard.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardCuarentena;

public class GetDashboardCuarentenaQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess)
    : IRequestHandler<GetDashboardCuarentenaQuery, Result<DashboardCuarentenaDto>>
{
    public async Task<Result<DashboardCuarentenaDto>> Handle(GetDashboardCuarentenaQuery request, CancellationToken cancellationToken)
    {
        var scope = await DashboardScopeResolver.ResolveAsync(context, sucursalAccess, cancellationToken);
        var ids = scope.Ids;

        // One lot counts once per branch even when it was isolated in several steps
        var retenidos = await context.DigemidInventarioCuarentena
            .AsNoTracking()
            .Where(q => ids.Contains(q.SucursalId) && q.EstadoResolucion == EstadoResolucionCuarentena.Retenido)
            .Select(q => new { q.SucursalId, q.LoteId })
            .Distinct()
            .CountAsync(cancellationToken);

        return Result.Success(new DashboardCuarentenaDto(retenidos));
    }
}
