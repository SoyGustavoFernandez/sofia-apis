using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Transferencias.Commands.RecibirTransferencia;

public class RecibirTransferenciaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<RecibirTransferenciaCommand, Result>
{
    public async Task<Result> Handle(RecibirTransferenciaCommand request, CancellationToken cancellationToken)
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

        if (sucursalResult.Value != transferencia.SucursalDestinoId)
        {
            return Result.Failure(Error.Forbidden("Transferencia.Forbidden", "Only staff from the destination branch can receive this transfer."));
        }

        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return Result.Failure(empleadoResult.Error);
        }

        var empleadoReceptorId = empleadoResult.Value;

        var recepcionesList = request.Recepciones.Select(r => (r.LoteId, r.CantidadRecibida)).ToList();
        var receiveResult = transferencia.Recibir(empleadoReceptorId, recepcionesList);
        if (receiveResult.IsFailure)
        {
            return receiveResult;
        }

        var stockResult = await UpdateDestinationInventoryAsync(transferencia, cancellationToken);
        if (stockResult.IsFailure)
        {
            return stockResult;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }

    private async Task<Result> UpdateDestinationInventoryAsync(Transferencia transferencia, CancellationToken cancellationToken)
    {
        foreach (var detalle in transferencia.Detalles)
        {
            var cantidadARecibir = detalle.CantidadRecibida ?? 0;
            if (cantidadARecibir <= 0)
            {
                continue;
            }

            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detalle.LoteId && x.SucursalId == transferencia.SucursalDestinoId, cancellationToken);

            if (inventario != null)
            {
                inventario.AddStock(cantidadARecibir);
                continue;
            }

            var newInventarioResult = InventarioSucursal.Create(transferencia.SucursalDestinoId, detalle.LoteId, cantidadARecibir);
            if (newInventarioResult.IsFailure)
            {
                return Result.Failure(newInventarioResult.Error);
            }

            _ = context.LotesEnSucursal.Add(newInventarioResult.Value!);
        }

        return Result.Success();
    }
}
