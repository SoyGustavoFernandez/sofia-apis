using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Commands.CreateVenta;

public record CreateVentaCommand(
    Guid? ClienteId,
    Guid? SesionId,
    List<CreateVentaDetailDto> Detalles,
    EstadoVenta Estado = EstadoVenta.Completada) : IRequest<Result<Guid>>;

public record CreateVentaDetailDto(
    Guid LoteId,
    decimal Cantidad,
    decimal PrecioUnitario,
    decimal CostoHistorico,
    Guid? RecetaId = null);

public class CreateVentaCommandValidator : AbstractValidator<CreateVentaCommand>
{
    public CreateVentaCommandValidator()
    {
        _ = RuleFor(v => v.Detalles)
            .NotEmpty().WithMessage("A sale must have at least one detail.");

        _ = RuleForEach(v => v.Detalles).ChildRules(detail =>
        {
            _ = detail.RuleFor(d => d.LoteId).NotEmpty();
            _ = detail.RuleFor(d => d.Cantidad).GreaterThan(0);
            _ = detail.RuleFor(d => d.PrecioUnitario).GreaterThanOrEqualTo(0);
        });
    }
}

public class CreateVentaCommandHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<CreateVentaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateVentaCommand request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId) || string.IsNullOrEmpty(currentUser.Id))
        {
            return Result.Failure<Guid>(Error.Unauthorized("Venta.Auth", "User must be authenticated and assigned to a branch."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out var sucursalId))
        {
            return Result.Failure<Guid>(Error.Validation("Venta.Sucursal", "Invalid Sucursal ID in user context."));
        }

        if (!Guid.TryParse(currentUser.Id, out var empleadoId))
        {
            return Result.Failure<Guid>(Error.Validation("Venta.Empleado", "Invalid Empleado ID in user context."));
        }

        List<DetalleVenta> detallesVenta = [];

        // 1. Validar y preparar detalles
        foreach (var detailDto in request.Detalles)
        {
            // Validar existencia del lote y stock en la sucursal actual
            var inventario = await context.LotesEnSucursal
                .FirstOrDefaultAsync(x => x.LoteId == detailDto.LoteId && x.SucursalId == sucursalId, cancellationToken);

            if (inventario == null)
            {
                return Result.Failure<Guid>(Error.NotFound("Venta.Lote", $"El lote {detailDto.LoteId} no existe en esta sucursal."));
            }

            if (inventario.CantidadFisica < detailDto.Cantidad)
            {
                return Result.Failure<Guid>(Error.Validation("Venta.Stock", $"Stock insuficiente para el lote {detailDto.LoteId}. Disponible: {inventario.CantidadFisica}"));
            }

            var detailResult = DetalleVenta.Create(
                detailDto.LoteId,
                detailDto.Cantidad,
                detailDto.PrecioUnitario,
                detailDto.CostoHistorico,
                detailDto.RecetaId);

            if (!detailResult.IsSuccess)
            {
                return Result.Failure<Guid>(detailResult.Error);
            }

            // 2. Descontar stock
            inventario.UpdateStock(inventario.CantidadFisica - detailDto.Cantidad);

            detallesVenta.Add(detailResult.Value);
        }

        // 3. Crear la venta
        var ventaResult = Venta.Create(
            sucursalId,
            empleadoId,
            request.ClienteId,
            request.SesionId,
            detallesVenta,
            request.Estado);

        if (!ventaResult.IsSuccess)
        {
            return Result.Failure<Guid>(ventaResult.Error);
        }

        _ = context.Ventas.Add(ventaResult.Value);

        // 4. Persistir cambios
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(ventaResult.Value.Id, 201);
    }
}
