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
        // 1. Validar Venta original
        var venta = await dbContext.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == request.ComprobanteOrigenId, cancellationToken);

        if (venta == null)
        {
            return Result.Failure<Guid>(Error.NotFound("Venta.NotFound", "La venta original no fue encontrada."));
        }

        // 2. Construir lista de detalles de dominio
        var domainDetalles = new List<DevolucionDetalle>();
        foreach (var dto in request.Detalles)
        {
            var ventaDetalle = venta.Detalles.FirstOrDefault(d => d.Id == dto.DetalleVentaId);
            if (ventaDetalle == null)
            {
                return Result.Failure<Guid>(Error.NotFound("DetalleVenta.NotFound", $"Detalle de venta {dto.DetalleVentaId} no encontrado."));
            }

            if (dto.CantidadDevuelta > ventaDetalle.CantidadVendida)
            {
                return Result.Failure<Guid>(Error.Validation("Devolucion.Cantidad", $"La cantidad devuelta no puede superar la cantidad vendida en el detalle {dto.DetalleVentaId}"));
            }

            var detalleResult = DevolucionDetalle.Create(dto.DetalleVentaId, dto.CantidadDevuelta, dto.DestinoFisicoLogico);
            if (detalleResult.IsFailure)
            {
                return Result.Failure<Guid>(detalleResult.Error);
            }

            domainDetalles.Add(detalleResult.Value!);
        }

        // 3. Crear Cabecera
        var cabeceraResult = DevolucionCabecera.Create(
            request.ComprobanteOrigenId,
            Guid.Empty, // ComprobanteNcId (por ahora Empty, generaremos NC abajo y lo actualizaremos)
            request.EmpleadoAutorizaId,
            request.MotivoSunatCatalogo,
            request.SustentoDescriptivo,
            DateTime.UtcNow,
            domainDetalles);

        if (cabeceraResult.IsFailure)
        {
            return Result.Failure<Guid>(cabeceraResult.Error);
        }

        var devolucion = cabeceraResult.Value;

        // 4. Actualizar InventarioSucursal
        foreach (var dto in request.Detalles)
        {
            var ventaDetalle = venta.Detalles.First(d => d.Id == dto.DetalleVentaId);

            // Reingreso de stock si aplica
            if (dto.DestinoFisicoLogico == Domain.Enums.DestinoDevolucion.Reingreso_Venta)
            {
                var inventario = await dbContext.LotesEnSucursal
                    .FirstOrDefaultAsync(i => i.SucursalId == venta.SucursalId && i.LoteId == ventaDetalle.LoteId, cancellationToken);

                if (inventario != null)
                {
                    inventario.AddStock(dto.CantidadDevuelta);
                    _ = dbContext.LotesEnSucursal.Update(inventario);
                }
            }
        }

        _ = _ = dbContext.Devoluciones.Add(devolucion!);

        // Generar Nota de Crédito
        var serie = await dbContext.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.SucursalId == venta.SucursalId && s.TipoComprobante == Domain.Enums.TipoComprobante.NotaCredito && s.EstadoSerie == "Activa", cancellationToken);

        if (serie != null)
        {
            var correlativo = serie.CorrelativoActual + 1;
            _ = serie.Update(serie.SucursalId, serie.TipoComprobante, serie.PrefijoSerie, correlativo, serie.EstadoSerie);
            _ = dbContext.SUNATSeriesFiscales.Update(serie);

            var ncResult = SUNATComprobanteEmitido.Create(
                venta.Id,
                serie.Id,
                correlativo,
                "1", // DNI as default for test
                "00000000",
                "CLIENTE VARIOS",
                venta.MontoTotalBruto * 0.82m,
                0m,
                venta.MontoTotalBruto * 0.18m,
                venta.MontoTotalBruto,
                null,
                "Aceptado",
                null,
                null,
                null,
                DateTime.UtcNow
            );

            if (ncResult.IsSuccess)
            {
                _ = _ = dbContext.SUNATComprobantesEmitidos.Add(ncResult.Value!);
                _ = devolucion!.Update(devolucion.ComprobanteOrigenId, ncResult.Value!.Id, devolucion.EmpleadoAutorizaId, devolucion.MotivoSunatCatalogo, devolucion.SustentoDescriptivo, devolucion.FechaDevolucion);
            }
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(devolucion!.Id);
    }
}
