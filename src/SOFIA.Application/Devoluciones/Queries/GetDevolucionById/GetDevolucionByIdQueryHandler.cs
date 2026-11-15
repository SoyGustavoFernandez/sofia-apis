using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Devoluciones.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Devoluciones.Queries.GetDevolucionById;

public class GetDevolucionByIdQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess) : IRequestHandler<GetDevolucionByIdQuery, Result<DevolucionConDetalleDto>>
{
    private const string ProductoDesconocido = "Producto desconocido";
    private const string SinDato = "N/A";

    public async Task<Result<DevolucionConDetalleDto>> Handle(GetDevolucionByIdQuery request, CancellationToken cancellationToken)
    {
        var allowed = await sucursalAccess.GetAllowedSucursalesAsync(cancellationToken);
        var devolucion = await context.Devoluciones
            .AsNoTracking()
            .WhereSucursalIn(d => context.SUNATComprobantesEmitidos.Where(c => c.Id == d.ComprobanteOrigenId).Select(c => c.Transaccion!.SucursalId).FirstOrDefault(), allowed)
            .FirstOrDefaultAsync(d => d.Id == request.Id && !d.IsDeleted, cancellationToken);

        if (devolucion == null)
        {
            return Result.Failure<DevolucionConDetalleDto>(Error.NotFound("Devolucion.NotFound", "Devolucion not found."), 404);
        }

        var resumen = (await context.GetResumenesAsync([devolucion], cancellationToken))[0];

        return Result.Success(DevolucionConDetalleDto.From(resumen, await GetLineasAsync(devolucion.Id, cancellationToken)));
    }

    // Loaded without crossing required navigations: a lot or product deleted after the sale must not make its returned line vanish from the history
    private async Task<List<DevolucionLineaDto>> GetLineasAsync(Guid devolucionId, CancellationToken cancellationToken)
    {
        var lineas = await context.DetallesDevolucion
            .AsNoTracking()
            .Where(l => l.DevolucionId == devolucionId && !l.IsDeleted)
            .OrderBy(l => l.CreatedAt)
            .ToListAsync(cancellationToken);

        var detalleIds = lineas.Select(l => l.DetalleVentaId).Distinct().ToList();
        var detallesVenta = await context.DetallesVenta
            .AsNoTracking()
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Where(d => detalleIds.Contains(d.Id))
            .Select(d => new { d.Id, d.LoteId, d.PrecioFijadoUnidad, d.PresentacionVentaId })
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        var loteIds = detallesVenta.Values.Select(d => d.LoteId).Distinct().ToList();
        var lotes = await context.LotesInventario
            .AsNoTracking()
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Where(l => loteIds.Contains(l.Id))
            .Select(l => new { l.Id, l.NumeroLoteMfr, l.ProductoId })
            .ToDictionaryAsync(l => l.Id, cancellationToken);

        var productoIds = lotes.Values.Select(l => l.ProductoId).Distinct().ToList();
        var productos = await context.Medicamentos
            .AsNoTracking()
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Where(m => productoIds.Contains(m.Id))
            .Select(m => new { m.Id, m.NombreComercial })
            .ToDictionaryAsync(m => m.Id, m => m.NombreComercial, cancellationToken);

        var presentacionIds = detallesVenta.Values.Where(d => d.PresentacionVentaId.HasValue).Select(d => d.PresentacionVentaId!.Value).Distinct().ToList();
        var presentaciones = await context.PresentacionesVenta
            .AsNoTracking()
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .Where(p => presentacionIds.Contains(p.Id))
            .Select(p => new { p.Id, p.Descripcion })
            .ToDictionaryAsync(p => p.Id, p => p.Descripcion, cancellationToken);

        return [.. lineas.Select(l =>
        {
            var detalle = detallesVenta.GetValueOrDefault(l.DetalleVentaId);
            var lote = detalle is null ? null : lotes.GetValueOrDefault(detalle.LoteId);
            var precio = detalle?.PrecioFijadoUnidad ?? 0m;

            return new DevolucionLineaDto(
                l.Id,
                l.DetalleVentaId,
                lote is null ? ProductoDesconocido : productos.GetValueOrDefault(lote.ProductoId) ?? ProductoDesconocido,
                lote?.NumeroLoteMfr ?? SinDato,
                detalle?.PresentacionVentaId is { } presentacionId ? presentaciones.GetValueOrDefault(presentacionId) : null,
                l.CantidadDevuelta,
                precio,
                l.CantidadDevuelta * precio,
                l.DestinoFisicoLogico.ToString());
        })];
    }
}
