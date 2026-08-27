using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.IngredientesActivos.Commands.UpdateIngredienteActivo;

public record UpdateIngredienteActivoCommand(Guid Id, string DenominacionDci, string CodigoAtc) : ICommand;

public class UpdateIngredienteActivoCommandValidator : AbstractValidator<UpdateIngredienteActivoCommand>
{
    public UpdateIngredienteActivoCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.DenominacionDci)
            .NotEmpty().WithMessage("Denominación DCI is required.")
            .MaximumLength(255).WithMessage("Denominación DCI must not exceed 255 characters.");

        _ = RuleFor(v => v.CodigoAtc)
            .NotEmpty().WithMessage("Código ATC is required.")
            .MaximumLength(15).WithMessage("Código ATC must not exceed 15 characters.");
    }
}

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
