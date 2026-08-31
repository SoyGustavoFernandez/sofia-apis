using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.IngredientesActivos.Commands.UpdateIngredienteActivo;

public class UpdateIngredienteActivoCommandValidator : AbstractValidator<UpdateIngredienteActivoCommand>
{
    public UpdateIngredienteActivoCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.DenominacionDci)
            .NotEmpty().WithMessage("DenominaciÃ³n DCI is required.")
            .MaximumLength(255).WithMessage("DenominaciÃ³n DCI must not exceed 255 characters.");

        _ = RuleFor(v => v.CodigoAtc)
            .NotEmpty().WithMessage("CÃ³digo ATC is required.")
            .MaximumLength(15).WithMessage("CÃ³digo ATC must not exceed 15 characters.");
    }
}
