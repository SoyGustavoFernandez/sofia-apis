using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Devoluciones.Commands.ProcesarDevolucion;

public record DevolucionDetalleDto(Guid DetalleVentaId, decimal CantidadDevuelta, Domain.Enums.DestinoDevolucion DestinoFisicoLogico);

public record ProcesarDevolucionCommand(
    Guid ComprobanteOrigenId,
    Guid EmpleadoAutorizaId,
    string MotivoSunatCatalogo,
    string SustentoDescriptivo,
    List<DevolucionDetalleDto> Detalles
) : ICommand<Guid>;

public class ProcesarDevolucionCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<ProcesarDevolucionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProcesarDevolucionCommand request, CancellationToken cancellationToken)
    {
        var venta = await dbContext.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == request.ComprobanteOrigenId, cancellationToken);

        if (venta == null)
        {
            return Result.Failure<Guid>(Error.NotFound("Venta.NotFound", "La venta original no fue encontrada."));
        }

        var detallesResult = BuildDomainDetalles(request.Detalles, venta);
        if (detallesResult.IsFailure)
        {
            return Result.Failure<Guid>(detallesResult.Error);
        }

        var cabeceraResult = DevolucionCabecera.Create(
            request.ComprobanteOrigenId,
            Guid.Empty,
            request.EmpleadoAutorizaId,
            request.MotivoSunatCatalogo,
            request.SustentoDescriptivo,
            DateTime.UtcNow,
            detallesResult.Value!);

        if (cabeceraResult.IsFailure)
        {
            return Result.Failure<Guid>(cabeceraResult.Error);
        }

        var devolucion = cabeceraResult.Value;

        await RestoreInventoryAsync(request.Detalles, venta, cancellationToken);

        _ = dbContext.Devoluciones.Add(devolucion!);

        await GenerateCreditNoteAsync(venta, devolucion!, cancellationToken);

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(devolucion!.Id);
    }

    private static Result<List<DevolucionDetalle>> BuildDomainDetalles(List<DevolucionDetalleDto> dtos, Venta venta)
    {
        var domainDetalles = new List<DevolucionDetalle>();

        foreach (var dto in dtos)
        {
            var ventaDetalle = venta.Detalles.FirstOrDefault(d => d.Id == dto.DetalleVentaId);
            if (ventaDetalle == null)
            {
                return Result.Failure<List<DevolucionDetalle>>(Error.NotFound("DetalleVenta.NotFound", $"Detalle de venta {dto.DetalleVentaId} no encontrado."));
            }

            if (dto.CantidadDevuelta > ventaDetalle.CantidadVendida)
            {
                return Result.Failure<List<DevolucionDetalle>>(Error.Validation("Devolucion.Cantidad", $"La cantidad devuelta no puede superar la cantidad vendida en el detalle {dto.DetalleVentaId}"));
            }

            var detalleResult = DevolucionDetalle.Create(dto.DetalleVentaId, dto.CantidadDevuelta, dto.DestinoFisicoLogico);
            if (detalleResult.IsFailure)
            {
                return Result.Failure<List<DevolucionDetalle>>(detalleResult.Error);
            }

            domainDetalles.Add(detalleResult.Value!);
        }

        return Result.Success(domainDetalles);
    }

    private async Task RestoreInventoryAsync(List<DevolucionDetalleDto> dtos, Venta venta, CancellationToken cancellationToken)
    {
        foreach (var dto in dtos)
        {
            if (dto.DestinoFisicoLogico != Domain.Enums.DestinoDevolucion.Reingreso_Venta)
            {
                continue;
            }

            var ventaDetalle = venta.Detalles.First(d => d.Id == dto.DetalleVentaId);
            var inventario = await dbContext.LotesEnSucursal
                .FirstOrDefaultAsync(i => i.SucursalId == venta.SucursalId && i.LoteId == ventaDetalle.LoteId, cancellationToken);

            if (inventario != null)
            {
                inventario.AddStock(dto.CantidadDevuelta);
                _ = dbContext.LotesEnSucursal.Update(inventario);
            }
        }
    }

    private async Task GenerateCreditNoteAsync(Venta venta, DevolucionCabecera devolucion, CancellationToken cancellationToken)
    {
        var serie = await dbContext.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.SucursalId == venta.SucursalId && s.TipoComprobante == Domain.Enums.TipoComprobante.NotaCredito && s.EstadoSerie == "Activa", cancellationToken);

        if (serie == null)
        {
            return;
        }

        var correlativo = serie.CorrelativoActual + 1;
        _ = serie.Update(serie.SucursalId, serie.TipoComprobante, serie.PrefijoSerie, correlativo, serie.EstadoSerie);
        _ = dbContext.SUNATSeriesFiscales.Update(serie);

        var ncResult = SunatComprobanteEmitido.Create(
            venta.Id, serie.Id, correlativo,
            "1", "00000000", "CLIENTE VARIOS",
            venta.MontoTotalBruto * 0.82m, 0m,
            venta.MontoTotalBruto * 0.18m, venta.MontoTotalBruto,
            null, "Aceptado", null, null, null,
            DateTime.UtcNow);

        if (ncResult.IsSuccess)
        {
            _ = dbContext.SUNATComprobantesEmitidos.Add(ncResult.Value);
            _ = devolucion.Update(devolucion.ComprobanteOrigenId, ncResult.Value.Id, devolucion.EmpleadoAutorizaId, devolucion.MotivoSunatCatalogo, devolucion.SustentoDescriptivo, devolucion.FechaDevolucion);
        }
    }
}
