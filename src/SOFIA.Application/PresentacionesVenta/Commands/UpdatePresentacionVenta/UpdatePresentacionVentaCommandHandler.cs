using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.JerarquiasUoM.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.PresentacionesVenta.Commands.UpdatePresentacionVenta;

public class UpdatePresentacionVentaCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdatePresentacionVentaCommand, Result>
{
    public async Task<Result> Handle(UpdatePresentacionVentaCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.PresentacionesVenta
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("PresentacionVenta.NotFound", $"Presentacion de Venta with ID {request.Id} was not found."), 404);
        }

        var yaExiste = await context.PresentacionesVenta
            .AnyAsync(x => x.Id != request.Id && x.ProductoId == entity.ProductoId && x.UnidadVentaId == request.UnidadVentaId, cancellationToken);
        if (yaExiste)
        {
            return Result.Failure(Error.Conflict("PresentacionVenta.UnidadVenta.Duplicada", "This product already has a sale presentation for that unit."), 409);
        }

        var producto = await context.Medicamentos
            .FirstOrDefaultAsync(x => x.Id == entity.ProductoId && !x.IsDeleted, cancellationToken);
        if (producto == null)
        {
            return Result.Failure(Error.NotFound("PresentacionVenta.Producto", $"Medicamento with ID {entity.ProductoId} was not found."), 404);
        }

        var unidadVenta = await context.UnidadesMedida
            .FirstOrDefaultAsync(x => x.Id == request.UnidadVentaId && !x.IsDeleted, cancellationToken);
        if (unidadVenta == null)
        {
            return Result.Failure(Error.NotFound("PresentacionVenta.UnidadVenta", $"Unidad de Medida with ID {request.UnidadVentaId} was not found."), 404);
        }

        var edges = await context.JerarquiasUoM
            .Where(x => x.ProductoId == entity.ProductoId)
            .Select(x => new JerarquiaConversionResolver.Edge(x.UnidadMayorId, x.UnidadMenorId, x.Multiplicador))
            .ToListAsync(cancellationToken);

        var cantidadUnidadesBase = JerarquiaConversionResolver.Resolve(edges, request.UnidadVentaId, producto.UnidadBaseId);
        if (cantidadUnidadesBase == null)
        {
            return Result.Failure(
                Error.Validation(
                    "PresentacionVenta.Jerarquia.NoResuelta",
                    "There is no unit conversion registered between the chosen sale unit and this product's base unit. Register the Jerarquía de Unidades first."));
        }

        var result = entity.Update(
            request.UnidadVentaId,
            unidadVenta.Descripcion,
            cantidadUnidadesBase.Value,
            request.PrecioVenta);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
