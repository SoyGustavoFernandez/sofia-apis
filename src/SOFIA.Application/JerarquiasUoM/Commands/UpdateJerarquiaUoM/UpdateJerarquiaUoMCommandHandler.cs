using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.JerarquiasUoM.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Commands.UpdateJerarquiaUoM;

public class UpdateJerarquiaUoMCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateJerarquiaUoMCommand, Result>
{
    public async Task<Result> Handle(UpdateJerarquiaUoMCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.JerarquiasUoM
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("JerarquiaUoM.NotFound", $"JerarquÃ­a with ID {request.Id} was not found."), 404);
        }

        // Tenant-filtered lookups: an id from another company must not be stored as a reference
        if (entity.ProductoId != request.ProductoId
            && !await context.Medicamentos.AnyAsync(m => m.Id == request.ProductoId, cancellationToken))
        {
            return Result.Failure(Error.NotFound("Medicamento.NotFound", "The specified product does not exist."), 404);
        }

        if ((entity.UnidadMayorId != request.UnidadMayorId && !await context.UnidadesMedida.AnyAsync(u => u.Id == request.UnidadMayorId, cancellationToken))
            || (entity.UnidadMenorId != request.UnidadMenorId && !await context.UnidadesMedida.AnyAsync(u => u.Id == request.UnidadMenorId, cancellationToken)))
        {
            return Result.Failure(Error.NotFound("UnidadMedida.NotFound", "The specified unit of measure does not exist."), 404);
        }

        var existingEdges = await context.JerarquiasUoM
            .Where(x => x.ProductoId == request.ProductoId && x.Id != request.Id)
            .Select(x => new JerarquiaConversionResolver.Edge(x.UnidadMayorId, x.UnidadMenorId, x.Multiplicador))
            .ToListAsync(cancellationToken);

        if (existingEdges.Exists(e => e.UnidadMayorId == request.UnidadMayorId && e.UnidadMenorId == request.UnidadMenorId))
        {
            return Result.Failure(Error.Conflict("JerarquiaUoM.Duplicada", "This product already has a conversion between these units."), 409);
        }

        var yaResuelto = JerarquiaConversionResolver.Resolve(existingEdges, request.UnidadMayorId, request.UnidadMenorId);
        if (yaResuelto.HasValue && decimal.Round(yaResuelto.Value, 4) != decimal.Round(request.Multiplicador, 4))
        {
            return Result.Failure(
                Error.Conflict(
                    "JerarquiaUoM.Multiplicador.Inconsistente",
                    $"A conversion between these units already exists for this product (computed: {decimal.Round(yaResuelto.Value, 4)}, entered: {request.Multiplicador})."),
                409);
        }

        var result = entity.Update(
            request.ProductoId,
            request.UnidadMayorId,
            request.UnidadMenorId,
            request.Multiplicador);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
