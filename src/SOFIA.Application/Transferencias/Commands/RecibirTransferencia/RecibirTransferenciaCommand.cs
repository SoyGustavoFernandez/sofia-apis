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
        _ = RuleFor(v => v.Id).NotEmpty().WithMessage("El ID de transferencia es requerido.");
        _ = RuleFor(v => v.Recepciones).NotEmpty().WithMessage("Debe proporcionar al menos una recepción de lote.");
        _ = RuleForEach(v => v.Recepciones).ChildRules(detail =>
        {
            _ = detail.RuleFor(d => d.LoteId).NotEmpty().WithMessage("El lote ID es requerido.");
            _ = detail.RuleFor(d => d.CantidadRecibida).GreaterThanOrEqualTo(0).WithMessage("La cantidad recibida no puede ser negativa.");
        });
    }
}

public class RecibirTransferenciaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<RecibirTransferenciaCommand, Result>
{
    public async Task<Result> Handle(RecibirTransferenciaCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener transferencia con detalles
        var transferencia = await context.Transferencias
            .Include(t => t.Detalles)
            .FirstOrDefaultAsync(t => t.Id == request.Id && !t.IsDeleted, cancellationToken);

        if (transferencia == null)
        {
            return Result.Failure(Error.NotFound("Transferencia.NotFound", $"La transferencia con ID {request.Id} no existe."));
        }

        // 2. Verificar autorización (debe pertenecer a la sucursal de destino)
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId) || string.IsNullOrEmpty(currentUser.Id))
        {
            return Result.Failure(Error.Unauthorized("Transferencia.Auth", "El usuario debe estar autenticado."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out var userSucursalId) || userSucursalId != transferencia.SucursalDestinoId)
        {
            return Result.Failure(Error.Forbidden("Transferencia.Forbidden", "Solo personal de la sucursal de destino puede recibir esta transferencia."));
        }

        if (!Guid.TryParse(currentUser.Id, out var empleadoReceptorId))
        {
            return Result.Failure(Error.Validation("Transferencia.EmpleadoReceptor", "ID de empleado receptor inválido."));
        }

        // 3. Modificar estado y registrar recepciones en la entidad
        var recepcionesList = request.Recepciones.Select(r => (r.LoteId, r.CantidadRecibida)).ToList();
        var receiveResult = transferencia.Recibir(empleadoReceptorId, recepcionesList);
        if (receiveResult.IsFailure)
        {
            return receiveResult;
        }

        // 4. Aumentar stock de la sucursal de destino
        foreach (var detalle in transferencia.Detalles)
        {
            var cantidadARecibir = detalle.CantidadRecibida ?? 0;
            if (cantidadARecibir > 0)
            {
                var inventario = await context.LotesEnSucursal
                    .FirstOrDefaultAsync(x => x.LoteId == detalle.LoteId && x.SucursalId == transferencia.SucursalDestinoId, cancellationToken);

                if (inventario != null)
                {
                    inventario.AddStock(cantidadARecibir);
                }
                else
                {
                    // Crear nuevo registro de stock en la sucursal de destino si no existía antes
                    var newInventarioResult = InventarioSucursal.Create(transferencia.SucursalDestinoId, detalle.LoteId, cantidadARecibir);
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
