using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.JerarquiasUoM.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.JerarquiasUoM.Commands.CreateJerarquiaUoM;

public class CreateJerarquiaUoMCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateJerarquiaUoMCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateJerarquiaUoMCommand request, CancellationToken cancellationToken)
    {
        var result = JerarquiaUoM.Create(
            request.ProductoId,
            request.UnidadMayorId,
            request.UnidadMenorId,
            request.Multiplicador);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        var existingEdges = await context.JerarquiasUoM
            .Where(x => x.ProductoId == request.ProductoId)
            .Select(x => new JerarquiaConversionResolver.Edge(x.UnidadMayorId, x.UnidadMenorId, x.Multiplicador))
            .ToListAsync(cancellationToken);

        // A path may already connect these two units through other units (e.g. Caja -> Blister -> Unidad).
        // Adding a direct edge is fine if it agrees with that path, but a contradicting value would let the
        // conversion silently depend on which edge happens to get picked, so it's rejected instead.
        var yaResuelto = JerarquiaConversionResolver.Resolve(existingEdges, request.UnidadMayorId, request.UnidadMenorId);
        if (yaResuelto.HasValue && decimal.Round(yaResuelto.Value, 4) != decimal.Round(request.Multiplicador, 4))
        {
            return Result.Failure<Guid>(
                Error.Conflict(
                    "JerarquiaUoM.Multiplicador.Inconsistente",
                    $"A conversion between these units already exists for this product (computed: {decimal.Round(yaResuelto.Value, 4)}, entered: {request.Multiplicador})."),
                409);
        }

        _ = context.JerarquiasUoM.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
