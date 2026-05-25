using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Transferencias.Commands.CancelarTransferencia;

public record CancelarTransferenciaCommand(Guid Id) : IRequest<Result>;

public class CancelarTransferenciaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CancelarTransferenciaCommand, Result>
{
    public async Task<Result> Handle(CancelarTransferenciaCommand request, CancellationToken cancellationToken)
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
            return Result.Failure(Error.Forbidden("Transferencia.Forbidden", "Solo personal de la sucursal de origen puede cancelar esta transferencia."));
        }

        var estadoAnterior = transferencia.EstadoLogistico;

        // 3. Modificar estado
        var cancelResult = transferencia.Cancelar();
        if (cancelResult.IsFailure)
        {
            return cancelResult;
        }

        // 4. Si el estado anterior era En_Transito, devolver stock a la sucursal de origen
        if (estadoAnterior == EstadoLogistico.En_Transito)
        {
            foreach (var detalle in transferencia.Detalles)
            {
                var inventario = await context.LotesEnSucursal
                    .FirstOrDefaultAsync(x => x.LoteId == detalle.LoteId && x.SucursalId == transferencia.SucursalOrigenId, cancellationToken);

                if (inventario != null)
                {
                    inventario.AddStock(detalle.CantidadEnviada);
                }
                else
                {
                    // Por consistencia, si por algún motivo no existía (ej. borrado manual erróneo), lo recreamos
                    var newInventarioResult = InventarioSucursal.Create(transferencia.SucursalOrigenId, detalle.LoteId, detalle.CantidadEnviada);
                    if (newInventarioResult.IsFailure)
                    {
                        return Result.Failure(newInventarioResult.Error);
                    }

                    _ = context.LotesEnSucursal.Add(newInventarioResult.Value!);
                }
            }
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
