using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Dashboard.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardDevoluciones;

public class GetDashboardDevolucionesQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess)
    : IRequestHandler<GetDashboardDevolucionesQuery, Result<DashboardDevolucionesDto>>
{
    public async Task<Result<DashboardDevolucionesDto>> Handle(GetDashboardDevolucionesQuery request, CancellationToken cancellationToken)
    {
        var scope = await DashboardScopeResolver.ResolveAsync(context, sucursalAccess, cancellationToken);
        var ids = scope.Ids;
        var ventana = DashboardDias.Ventana(request.Hoy);
        var desde = ventana.DesdeUtc;
        var hasta = ventana.HastaUtc;

        // A return belongs to the branch of the sale whose receipt it credits
        var devoluciones = context.Devoluciones
            .AsNoTracking()
            .Where(d => d.FechaDevolucion >= desde && d.FechaDevolucion < hasta)
            .Where(d => ids.Contains(context.SUNATComprobantesEmitidos.Where(c => c.Id == d.ComprobanteOrigenId).Select(c => c.Transaccion!.SucursalId).FirstOrDefault()));

        var cantidad = await devoluciones.CountAsync(cancellationToken);
        var montoReembolsado = await devoluciones.SumAsync(d => d.MontoReembolsado ?? 0m, cancellationToken);

        return Result.Success(new DashboardDevolucionesDto(cantidad, montoReembolsado));
    }
}
