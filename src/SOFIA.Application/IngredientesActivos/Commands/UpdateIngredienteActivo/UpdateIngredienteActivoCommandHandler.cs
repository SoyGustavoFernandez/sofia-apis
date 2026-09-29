using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
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

        var duplicado = await context.IngredientesActivos
            .AnyAsync(i => i.Id != entity.Id && i.DenominacionDci == request.DenominacionDci && !i.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure(Error.Conflict("IngredienteActivo.DenominacionDci.Duplicado", "Another active ingredient already uses this INN."), 409);
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
