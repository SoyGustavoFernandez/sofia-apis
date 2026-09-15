using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Ventas.Common;

public static class VentaDetalleUpdater
{
    public static async Task<Result> ReplaceAsync(IApplicationDbContext context, Venta venta, List<CreateVentaDetailDto> nuevosDtos, Guid? clienteId, Guid sucursalId, CancellationToken cancellationToken)
    {
        var detallesAnteriores = venta.Detalles.ToList();

        foreach (var detalle in detallesAnteriores)
        {
            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detalle.LoteId && x.SucursalId == sucursalId, cancellationToken);

            inventario?.UpdateStock(inventario.CantidadFisica + detalle.CantidadVendida);
        }

        var nuevosResult = await VentaDetalleFactory.BuildAsync(context, nuevosDtos, sucursalId, cancellationToken);
        if (nuevosResult.IsFailure)
        {
            return Result.Failure(nuevosResult.Error);
        }

        var actualizarResult = venta.ActualizarDetalles(nuevosResult.Value!, clienteId);
        if (!actualizarResult.IsSuccess)
        {
            return actualizarResult;
        }

        context.DetallesVenta.RemoveRange(detallesAnteriores);
        foreach (var nuevo in nuevosResult.Value!)
        {
            _ = context.DetallesVenta.Add(nuevo);
        }

        return Result.Success();
    }
}
