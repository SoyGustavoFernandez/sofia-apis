using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Ventas.Common;

public static class VentaDetalleUpdater
{
    // The value says whether the new lines need the pharmacist's prescription confirmation before the sale is charged
    public static async Task<Result<bool>> ReplaceAsync(IApplicationDbContext context, Venta venta, List<CreateVentaDetailDto> nuevosDtos, Guid? clienteId, Guid sucursalId, bool exigirRegistroReceta, CancellationToken cancellationToken)
    {
        // Tenant-filtered lookup: a patient of another company must not be stored as the customer
        if (clienteId is { } nuevoClienteId && nuevoClienteId != venta.ClienteId
            && !await context.Pacientes.AnyAsync(p => p.Id == nuevoClienteId, cancellationToken))
        {
            return Result.Failure<bool>(Error.NotFound("Paciente.NotFound", "The specified patient does not exist."), 404);
        }

        var detallesAnteriores = venta.Detalles.ToList();

        foreach (var detalle in detallesAnteriores)
        {
            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detalle.LoteId && x.SucursalId == sucursalId, cancellationToken);

            inventario?.UpdateStock(inventario.CantidadFisica + detalle.CantidadVendida);
        }

        var nuevosResult = await VentaDetalleFactory.BuildAsync(context, nuevosDtos, sucursalId, clienteId, venta.Id, exigirRegistroReceta, cancellationToken);
        if (nuevosResult.IsFailure)
        {
            return Result.Failure<bool>(nuevosResult.Error);
        }

        var actualizarResult = venta.ActualizarDetalles(nuevosResult.Value!.Detalles, clienteId);
        if (!actualizarResult.IsSuccess)
        {
            return Result.Failure<bool>(actualizarResult.Error);
        }

        context.DetallesVenta.RemoveRange(detallesAnteriores);
        foreach (var nuevo in nuevosResult.Value!.Detalles)
        {
            _ = context.DetallesVenta.Add(nuevo);
        }

        return Result.Success(nuevosResult.Value!.RequiereVerificacionReceta);
    }
}
