using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Devoluciones.Common;
using SOFIA.Application.Ventas.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Queries.GetVentaById;

public class GetVentaByIdQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetVentaByIdQuery, Result<VentaConDetalleDto>>
{
    public async Task<Result<VentaConDetalleDto>> Handle(GetVentaByIdQuery request, CancellationToken cancellationToken)
    {
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<VentaConDetalleDto>(sucursalResult.Error);
        }

        var sucursalId = sucursalResult.Value;

        var venta = await context.Ventas
            .AsNoTracking()
            .Include(v => v.Empleado)
            .Include(v => v.Cliente)
            .Include(v => v.Pagos)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.Lote)
                    .ThenInclude(l => l!.Producto)
            .Include(v => v.Detalles)
                .ThenInclude(d => d.PresentacionVenta)
            .FirstOrDefaultAsync(v => v.Id == request.Id, cancellationToken);

        var comprobante = await context.SUNATComprobantesEmitidos
            .Include(c => c.Serie)
            .Where(c => c.TransaccionId == request.Id && !c.IsDeleted)
            .Where(ComprobanteVentaFilters.EsBoletaOFactura)
            .OrderBy(c => c.FechaEmision)
            .FirstOrDefaultAsync(cancellationToken);

        if (venta == null)
        {
            return Result.Failure<VentaConDetalleDto>(Error.NotFound("Venta.NotFound", $"No se encontró la venta con ID {request.Id}"), 404);
        }

        if (venta.SucursalId != sucursalId)
        {
            return Result.Failure<VentaConDetalleDto>(Error.Forbidden("Venta.Forbidden", "No tiene permiso para ver esta venta."));
        }

        var cantidadesDevueltas = await context.GetCantidadesDevueltasAsync([.. venta.Detalles.Select(d => d.Id)], cancellationToken);
        var devoluciones = comprobante != null ? await GetDevolucionesAsync(comprobante.Id, cancellationToken) : [];
        var emisor = comprobante != null ? await VentaComprobanteGenerator.GetEmisorAsync(context, venta.SucursalId, cancellationToken) : null;

        var codigoVenta = comprobante != null
            ? $"{comprobante.Serie?.PrefijoSerie}-{comprobante.NumeroCorrelativo:D8}"
            : venta.Id.ToString()[..8].ToUpper();

        var dto = new VentaConDetalleDto(
            venta.Id,
            codigoVenta,
            venta.FechaHoraUtc,
            venta.MontoTotalBruto,
            venta.Estado.ToString(),
            venta.MotivoAnulacion,
            venta.Empleado != null ? $"{venta.Empleado.Nombres} {venta.Empleado.Apellido_Paterno}" : "N/A",
            venta.ClienteId,
            venta.Cliente?.NombreApellidos ?? "Público General",
            venta.Cliente?.DocIdentidadGub,
            [.. venta.Detalles.Select(d => new VentaDetalleDto(
                d.Id,
                d.LoteId,
                d.Lote?.Producto?.NombreComercial ?? "Producto desconocido",
                d.Lote?.NumeroLoteMfr ?? "N/A",
                d.CantidadVendida,
                d.PrecioFijadoUnidad,
                d.CantidadVendida * d.PrecioFijadoUnidad,
                d.PresentacionVentaId,
                d.PresentacionVenta?.Descripcion,
                d.CantidadEnPresentacion,
                d.RecetaId,
                cantidadesDevueltas.GetValueOrDefault(d.Id)
            ))],
            comprobante != null ? new ComprobanteDto(
                comprobante.Serie?.TipoComprobante.ToString() ?? "Boleta",
                $"{comprobante.Serie?.PrefijoSerie}-{comprobante.NumeroCorrelativo:D8}",
                comprobante.EstadoAceptacion,
                comprobante.UrlPublicaVerificacion,
                comprobante.RutaArchivoXml,
                comprobante.RutaArchivoCdr,
                emisor?.RazonSocial,
                emisor?.Ruc,
                emisor?.Direccion,
                comprobante.TipoDocIdentidadCliente,
                comprobante.NumeroIdentidadCliente,
                comprobante.RazonSocialCliente
            ) : null,
            [.. venta.Pagos.Select(p => new VentaPagoDto(
                p.Id,
                p.MetodoPago.ToString(),
                p.MontoPagado,
                p.ReferenciaOperacion,
                p.FechaPago
            ))],
            devoluciones,
            venta.Cliente?.TipoDocumento
        );

        return Result.Success(dto);
    }

    // Returns credit the sale's boleta/factura; each one carries the credit note issued for it
    private async Task<List<VentaDevolucionDto>> GetDevolucionesAsync(Guid comprobanteId, CancellationToken cancellationToken)
    {
        var devoluciones = await context.Devoluciones
            .AsNoTracking()
            .Where(d => d.ComprobanteOrigenId == comprobanteId && !d.IsDeleted)
            .OrderBy(d => d.FechaDevolucion)
            .ToListAsync(cancellationToken);

        var ncIds = devoluciones.Where(d => d.ComprobanteNcId.HasValue).Select(d => d.ComprobanteNcId!.Value).ToList();
        var notasCredito = await context.SUNATComprobantesEmitidos
            .AsNoTracking()
            .Include(c => c.Serie)
            .Where(c => ncIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        return [.. devoluciones.Select(d => new VentaDevolucionDto(
            d.Id,
            d.FechaDevolucion,
            d.ComprobanteNcId.HasValue && notasCredito.TryGetValue(d.ComprobanteNcId.Value, out var nc)
                ? $"{nc.Serie?.PrefijoSerie}-{nc.NumeroCorrelativo:D8}"
                : null,
            d.MontoReembolsado,
            d.MetodoReembolso?.ToString()))];
    }
}
