using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.JerarquiasUoM.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.PresentacionesVenta.Commands.CreatePresentacionVenta;

public class CreatePresentacionVentaCommandHandler(IApplicationDbContext context) : IRequestHandler<CreatePresentacionVentaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreatePresentacionVentaCommand request, CancellationToken cancellationToken)
    {
        var producto = await context.Medicamentos
            .FirstOrDefaultAsync(x => x.Id == request.ProductoId && !x.IsDeleted, cancellationToken);
        if (producto == null)
        {
            return Result.Failure<Guid>(Error.NotFound("PresentacionVenta.Producto", $"Medicamento with ID {request.ProductoId} was not found."), 404);
        }

        var yaExiste = await context.PresentacionesVenta
            .AnyAsync(x => x.ProductoId == request.ProductoId && x.UnidadVentaId == request.UnidadVentaId, cancellationToken);
        if (yaExiste)
        {
            return Result.Failure<Guid>(Error.Conflict("PresentacionVenta.UnidadVenta.Duplicada", "This product already has a sale presentation for that unit."), 409);
        }

        var unidadVenta = await context.UnidadesMedida
            .FirstOrDefaultAsync(x => x.Id == request.UnidadVentaId && !x.IsDeleted, cancellationToken);
        if (unidadVenta == null)
        {
            return Result.Failure<Guid>(Error.NotFound("PresentacionVenta.UnidadVenta", $"Unidad de Medida with ID {request.UnidadVentaId} was not found."), 404);
        }

        var edges = await context.JerarquiasUoM
            .Where(x => x.ProductoId == request.ProductoId)
            .Select(x => new JerarquiaConversionResolver.Edge(x.UnidadMayorId, x.UnidadMenorId, x.Multiplicador))
            .ToListAsync(cancellationToken);

        var cantidadUnidadesBase = JerarquiaConversionResolver.Resolve(edges, request.UnidadVentaId, producto.UnidadBaseId);
        if (cantidadUnidadesBase == null)
        {
            return Result.Failure<Guid>(
                Error.Validation(
                    "PresentacionVenta.Jerarquia.NoResuelta",
                    "There is no unit conversion registered between the chosen sale unit and this product's base unit. Register the Jerarquía de Unidades first."));
        }

        var result = PresentacionVenta.Create(
            request.ProductoId,
            request.UnidadVentaId,
            unidadVenta.Descripcion,
            cantidadUnidadesBase.Value,
            request.PrecioVenta);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.PresentacionesVenta.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
