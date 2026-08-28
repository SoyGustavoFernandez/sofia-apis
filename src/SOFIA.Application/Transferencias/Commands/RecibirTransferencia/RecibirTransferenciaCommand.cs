using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Transferencias.Commands.RecibirTransferencia;

public record RecepcionLoteInputDto(Guid LoteId, decimal CantidadRecibida);

public record RecibirTransferenciaCommand(
    Guid Id,
    List<RecepcionLoteInputDto> Recepciones) : ICommand;

public class RecibirTransferenciaCommandValidator : AbstractValidator<RecibirTransferenciaCommand>
{
    public RecibirTransferenciaCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty().WithMessage("Transfer ID is required.");
        _ = RuleFor(v => v.Recepciones).NotEmpty().WithMessage("At least one lot reception must be provided.");
        _ = RuleForEach(v => v.Recepciones).ChildRules(detail =>
        {
            _ = detail.RuleFor(d => d.LoteId).NotEmpty().WithMessage("El lote ID es requerido.");
            _ = detail.RuleFor(d => d.CantidadRecibida).GreaterThanOrEqualTo(0).WithMessage("Received quantity cannot be negative.");
        });
    }
}

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

        var authResult = ValidateAndParseUserContext(transferencia.SucursalDestinoId, out var userSucursalId, out var empleadoReceptorId);
        if (authResult.IsFailure)
        {
            return authResult;
        }

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

    private Result ValidateAndParseUserContext(Guid sucursalDestinoId, out Guid sucursalId, out Guid empleadoId)
    {
        sucursalId = Guid.Empty;
        empleadoId = Guid.Empty;

        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId) || string.IsNullOrEmpty(currentUser.Id))
        {
            return Result.Failure(Error.Unauthorized("Transferencia.Auth", "User must be authenticated."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out sucursalId) || sucursalId != sucursalDestinoId)
        {
            return Result.Failure(Error.Forbidden("Transferencia.Forbidden", "Only staff from the destination branch can receive this transfer."));
        }

        if (!Guid.TryParse(currentUser.Id, out empleadoId))
        {
            return Result.Failure(Error.Validation("Transferencia.EmpleadoReceptor", "Invalid receptor employee ID."));
        }

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
