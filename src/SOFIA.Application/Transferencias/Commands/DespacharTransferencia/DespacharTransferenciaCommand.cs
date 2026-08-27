using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Transferencias.Commands.DespacharTransferencia;

public record DespacharTransferenciaCommand(Guid Id) : ICommand;

public class DespacharTransferenciaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<DespacharTransferenciaCommand, Result>
{
    public async Task<Result> Handle(DespacharTransferenciaCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener transferencia con detalles
        var transferencia = await context.Transferencias
            .Include(t => t.Detalles)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);

        if (transferencia == null)
        {
            return Result.Failure(Error.NotFound("Transferencia.NotFound", $"La transferencia con ID {request.Id} no existe."));
        }

        // 2. Verificar autorización (debe pertenecer a la sucursal de origen)
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId))
        {
            return Result.Failure(Error.Unauthorized("Transferencia.Auth", "El usuario debe estar autenticado."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out var userSucursalId) || userSucursalId != transferencia.SucursalOrigenId)
        {
            return Result.Failure(Error.Forbidden("Transferencia.Forbidden", "Solo personal de la sucursal de origen puede despachar esta transferencia."));
        }

        // 3. Modificar estado en la entidad
        var dispatchResult = transferencia.Despachar();
        if (dispatchResult.IsFailure)
        {
            return dispatchResult;
        }

        // 4. Descontar stock de la sucursal de origen
        foreach (var detalle in transferencia.Detalles)
        {
            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detalle.LoteId && x.SucursalId == transferencia.SucursalOrigenId, cancellationToken);

            if (inventario == null || inventario.CantidadFisica < detalle.CantidadEnviada)
            {
                return Result.Failure(Error.Validation("Transferencia.StockInsuficiente", $"Stock insuficiente para despachar el lote {detalle.LoteId}. Disponible: {inventario?.CantidadFisica ?? 0}"));
            }

            // Descontar
            inventario.UpdateStock(inventario.CantidadFisica - detalle.CantidadEnviada);
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
