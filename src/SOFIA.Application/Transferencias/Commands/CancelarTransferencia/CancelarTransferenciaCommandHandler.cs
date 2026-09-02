using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Transferencias.Commands.CancelarTransferencia;

public class CancelarTransferenciaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CancelarTransferenciaCommand, Result>
{
    public async Task<Result> Handle(CancelarTransferenciaCommand request, CancellationToken cancellationToken)
    {
        var transferencia = await context.Transferencias
            .Include(t => t.Detalles)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);

        if (transferencia == null)
        {
            return Result.Failure(Error.NotFound("Transferencia.NotFound", $"La transferencia con ID {request.Id} no existe."));
        }

        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure(sucursalResult.Error);
        }

        if (sucursalResult.Value != transferencia.SucursalOrigenId)
        {
            return Result.Failure(Error.Forbidden("Transferencia.Forbidden", "Only staff from the origin branch can cancel this transfer."));
        }

        var estadoAnterior = transferencia.EstadoLogistico;

        var cancelResult = transferencia.Cancelar();
        if (cancelResult.IsFailure)
        {
            return cancelResult;
        }

        if (estadoAnterior == EstadoLogistico.En_Transito)
        {
            var stockResult = await RestoreOriginInventoryAsync(transferencia, cancellationToken);
            if (stockResult.IsFailure)
            {
                return stockResult;
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> RestoreOriginInventoryAsync(Transferencia transferencia, CancellationToken cancellationToken)
    {
        foreach (var detalle in transferencia.Detalles)
        {
            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detalle.LoteId && x.SucursalId == transferencia.SucursalOrigenId, cancellationToken);

            if (inventario != null)
            {
                inventario.AddStock(detalle.CantidadEnviada);
                continue;
            }

            var newInventarioResult = InventarioSucursal.Create(transferencia.SucursalOrigenId, detalle.LoteId, detalle.CantidadEnviada);
            if (newInventarioResult.IsFailure)
            {
                return Result.Failure(newInventarioResult.Error);
            }

            _ = context.LotesEnSucursal.Add(newInventarioResult.Value!);
        }

        return Result.Success();
    }
}
