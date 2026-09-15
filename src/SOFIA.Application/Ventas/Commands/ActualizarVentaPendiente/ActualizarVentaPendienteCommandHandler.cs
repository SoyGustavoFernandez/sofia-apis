using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Commands.ActualizarVentaPendiente;

public class ActualizarVentaPendienteCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<ActualizarVentaPendienteCommand, Result>
{
    public async Task<Result> Handle(ActualizarVentaPendienteCommand request, CancellationToken cancellationToken)
    {
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure(sucursalResult.Error);
        }

        var sucursalId = sucursalResult.Value;

        var venta = await context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == request.VentaId && !v.IsDeleted, cancellationToken);

        if (venta == null)
        {
            return Result.Failure(Error.NotFound("Venta.Actualizar", "Sale not found."));
        }

        if (venta.SucursalId != sucursalId)
        {
            return Result.Failure(Error.Forbidden("Venta.Actualizar", "You do not have permission to modify sales from another branch."));
        }

        if (venta.Estado != EstadoVenta.Pendiente)
        {
            return Result.Failure(Error.Validation("Venta.Actualizar", "Only a pending sale can have its items modified."));
        }

        var updateResult = await VentaDetalleUpdater.ReplaceAsync(context, venta, request.Detalles, request.ClienteId, sucursalId, cancellationToken);
        if (!updateResult.IsSuccess)
        {
            return updateResult;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
