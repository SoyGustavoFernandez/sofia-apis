using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Commands.AnularVenta;

public class AnularVentaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<AnularVentaCommand, Result>
{
    public async Task<Result> Handle(AnularVentaCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch the sale with its details
        var venta = await context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == request.VentaId, cancellationToken);

        if (venta == null)
        {
            return Result.Failure(Error.NotFound("Venta.Anular", "Sale not found."));
        }

        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure(sucursalResult.Error);
        }

        var sucursalId = sucursalResult.Value;
        if (venta.SucursalId != sucursalId)
        {
            return Result.Failure(Error.Forbidden("Venta.Anular", "You do not have permission to cancel sales from another branch."));
        }

        // Returned units were already restocked and credited, so voiding would restore them and refund them twice
        var detalleIds = venta.Detalles.Select(d => d.Id).ToList();
        var tieneDevoluciones = await (
            from dd in context.DetallesDevolucion
            join dc in context.Devoluciones on dd.DevolucionId equals dc.Id
            where detalleIds.Contains(dd.DetalleVentaId) && !dd.IsDeleted && !dc.IsDeleted
            select dd.Id).AnyAsync(cancellationToken);

        if (tieneDevoluciones)
        {
            return Result.Failure(Error.Validation("Venta.Anular.ConDevoluciones", "A sale with returns cannot be voided."));
        }

        // 3. Apply cancellation in the domain
        var resultAnular = venta.Anular(request.Motivo);
        if (!resultAnular.IsSuccess)
        {
            return Result.Failure(resultAnular.Error);
        }

        // 4. Revertir Stock
        foreach (var detalle in venta.Detalles)
        {
            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detalle.LoteId && x.SucursalId == sucursalId, cancellationToken);

            inventario?.UpdateStock(inventario.CantidadFisica + detalle.CantidadVendida);
        }

        // 5. Guardar cambios
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
