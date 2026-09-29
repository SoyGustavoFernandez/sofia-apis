using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.IngredientesActivos.Commands.CreateIngredienteActivo;

public class CreateIngredienteActivoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateIngredienteActivoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateIngredienteActivoCommand request, CancellationToken cancellationToken)
    {
        var result = IngredienteActivo.Create(request.DenominacionDci, request.CodigoAtc);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        var duplicado = await context.IngredientesActivos
            .AnyAsync(i => i.DenominacionDci == request.DenominacionDci && !i.IsDeleted, cancellationToken);
        if (duplicado)
        {
            return Result.Failure<Guid>(Error.Conflict("IngredienteActivo.DenominacionDci.Duplicado", "Another active ingredient already uses this INN."), 409);
        }

        _ = context.IngredientesActivos.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
