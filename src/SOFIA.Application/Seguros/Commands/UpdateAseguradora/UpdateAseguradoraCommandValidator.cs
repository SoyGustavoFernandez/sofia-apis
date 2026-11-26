using FluentValidation;

namespace SOFIA.Application.Seguros.Commands.UpdateAseguradora;

public class UpdateAseguradoraCommandValidator : AbstractValidator<UpdateAseguradoraCommand>
{
    public UpdateAseguradoraCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Insurer is required.");

        _ = RuleFor(v => v.NombreComercial)
            .NotEmpty().WithMessage("Commercial name is required.")
            .MaximumLength(150).WithMessage("Commercial name must not exceed 150 characters.");

        _ = RuleFor(v => v.CodigoIdentificadorNacional)
            .NotEmpty().WithMessage("National code is required.")
            .MaximumLength(50).WithMessage("National code must not exceed 50 characters.");
    }
}
