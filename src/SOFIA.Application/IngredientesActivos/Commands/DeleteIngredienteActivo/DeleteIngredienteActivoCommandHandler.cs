using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.IngredientesActivos.Commands.DeleteIngredienteActivo;

public class DeleteIngredienteActivoCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteIngredienteActivoCommand, Result>
{
    public async Task<Result> Handle(DeleteIngredienteActivoCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.IngredientesActivos
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("IngredienteActivo.NotFound", $"Ingrediente Activo with ID {request.Id} was not found."), 404);
        }

        var referenciadoPorFormulaciones = await context.FormulacionesClinicas
            .AnyAsync(f => f.IngredienteId == request.Id && !f.IsDeleted, cancellationToken);
        if (referenciadoPorFormulaciones)
        {
            return Result.Failure(
                Error.Conflict("IngredienteActivo.InUse", "Cannot delete an active ingredient referenced by clinical formulations."),
                409);
        }

        _ = context.IngredientesActivos.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}
