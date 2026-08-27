using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Commands.AnularVenta;

public record AnularVentaCommand(Guid VentaId, string Motivo) : ICommand;

public class AnularVentaCommandValidator : AbstractValidator<AnularVentaCommand>
{
    public AnularVentaCommandValidator()
    {
        _ = RuleFor(x => x.VentaId).NotEmpty();
        _ = RuleFor(x => x.Motivo).NotEmpty().MaximumLength(255);
    }
}

public class AnularVentaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<AnularVentaCommand, Result>
{
    public async Task<Result> Handle(AnularVentaCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener la venta con sus detalles
        var venta = await context.Ventas
            .Include(v => v.Detalles)
            .FirstOrDefaultAsync(v => v.Id == request.VentaId, cancellationToken);

        if (venta == null)
        {
            return Result.Failure(Error.NotFound("Venta.Anular", "La venta no existe."));
        }

        // 2. Seguridad: Solo se puede anular si pertenece a la sucursal del usuario
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId))
        {
            return Result.Failure(Error.Unauthorized("Venta.Auth", "Usuario no autenticado."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out var sucursalId) || venta.SucursalId != sucursalId)
        {
            return Result.Failure(Error.Forbidden("Venta.Anular", "No tiene permisos para anular ventas de otra sucursal."));
        }

        // 3. Aplicar anulación en el dominio
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
