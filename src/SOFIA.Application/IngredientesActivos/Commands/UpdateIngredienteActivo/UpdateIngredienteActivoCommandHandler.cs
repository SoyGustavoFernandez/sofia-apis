using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.IngredientesActivos.Commands.UpdateIngredienteActivo;

public class UpdateIngredienteActivoCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateIngredienteActivoCommand, Result>
{
    public async Task<Result> Handle(UpdateIngredienteActivoCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.IngredientesActivos
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("IngredienteActivo.NotFound", $"Ingrediente Activo with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(request.DenominacionDci, request.CodigoAtc);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
