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
                .FirstOrDefaultAsync(x => x.LoteId == detailDto.LoteId && x.SucursalId == sucursalId, cancellationToken);

            if (inventario == null)
            {
                return Result.Failure<List<DetalleVenta>>(Error.NotFound("Venta.Lote", $"El lote {detailDto.LoteId} no existe en esta sucursal."));
            }

            if (inventario.CantidadFisica < detailDto.Cantidad)
            {
                return Result.Failure<List<DetalleVenta>>(Error.Validation("Venta.Stock", $"Stock insuficiente para el lote {detailDto.LoteId}. Disponible: {inventario.CantidadFisica}"));
            }

            var detailResult = DetalleVenta.Create(detailDto.LoteId, detailDto.Cantidad, detailDto.PrecioUnitario, detailDto.CostoHistorico, detailDto.RecetaId);
            if (!detailResult.IsSuccess)
            {
                return Result.Failure<List<DetalleVenta>>(detailResult.Error);
            }

            inventario.UpdateStock(inventario.CantidadFisica - detailDto.Cantidad);
            detallesVenta.Add(detailResult.Value);
        }

        return Result.Success(detallesVenta);
    }
}
