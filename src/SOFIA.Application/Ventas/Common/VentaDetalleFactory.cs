using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Ventas.Common;

public static class VentaDetalleFactory
{
    public static async Task<Result<List<DetalleVenta>>> BuildAsync(IApplicationDbContext context, List<CreateVentaDetailDto> dtos, Guid sucursalId, CancellationToken cancellationToken)
    {
        var detallesVenta = new List<DetalleVenta>();

        foreach (var detailDto in dtos)
        {
            var enCuarentena = await context.DigemidInventarioCuarentena
                .AnyAsync(q => q.LoteId == detailDto.LoteId && q.EstadoResolucion == "Retenido" && !q.IsDeleted, cancellationToken);

            if (enCuarentena)
            {
                return Result.Failure<List<DetalleVenta>>(Error.Validation("Venta.Cuarentena", $"Lot {detailDto.LoteId} is in quarantine and cannot be sold under any circumstances."));
            }

            var inventario = await context.LotesEnSucursal
                .Include(x => x.Lote)
                .FirstOrDefaultAsync(x => x.LoteId == detailDto.LoteId && x.SucursalId == sucursalId, cancellationToken);

            if (inventario == null)
            {
                return Result.Failure<List<DetalleVenta>>(Error.NotFound("Venta.Lote", $"El lote {detailDto.LoteId} no existe en esta sucursal."));
            }

            // Cantidad is expressed in base units unless a sale presentation (e.g. "Caja x10") was
            // picked, in which case the backend — not the client — resolves the conversion factor,
            // so a stale/tampered client can't misreport how much stock a sale actually consumes.
            var cantidadBase = detailDto.Cantidad;
            Guid? presentacionId = null;
            decimal? cantidadEnPresentacion = null;

            if (detailDto.PresentacionVentaId.HasValue)
            {
                var presentacion = await context.PresentacionesVenta
                    .FirstOrDefaultAsync(p => p.Id == detailDto.PresentacionVentaId.Value && !p.IsDeleted, cancellationToken);

                if (presentacion == null)
                {
                    return Result.Failure<List<DetalleVenta>>(Error.NotFound("Venta.Presentacion", $"La presentacion de venta {detailDto.PresentacionVentaId.Value} no existe."));
                }

                if (inventario.Lote != null && presentacion.ProductoId != inventario.Lote.ProductoId)
                {
                    return Result.Failure<List<DetalleVenta>>(Error.Validation("Venta.Presentacion", "La presentacion seleccionada no corresponde al producto del lote."));
                }

                cantidadBase = detailDto.Cantidad * presentacion.CantidadUnidadesBase;
                presentacionId = presentacion.Id;
                cantidadEnPresentacion = detailDto.Cantidad;
            }

            if (inventario.CantidadFisica < cantidadBase)
            {
                return Result.Failure<List<DetalleVenta>>(Error.Validation("Venta.Stock", $"Stock insuficiente para el lote {detailDto.LoteId}. Disponible: {inventario.CantidadFisica}"));
            }

            var detailResult = DetalleVenta.Create(detailDto.LoteId, cantidadBase, detailDto.PrecioUnitario, detailDto.CostoHistorico, detailDto.RecetaId, presentacionId, cantidadEnPresentacion);
            if (!detailResult.IsSuccess)
            {
                return Result.Failure<List<DetalleVenta>>(detailResult.Error);
            }

            inventario.UpdateStock(inventario.CantidadFisica - cantidadBase);
            detallesVenta.Add(detailResult.Value);
        }

        return Result.Success(detallesVenta);
    }
}
