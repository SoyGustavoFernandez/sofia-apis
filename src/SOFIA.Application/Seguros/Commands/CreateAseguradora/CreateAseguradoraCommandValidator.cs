using FluentValidation;

namespace SOFIA.Application.Seguros.Commands.CreateAseguradora;

// Validator = pipeline fast-fail; entity method = domain invariant. Both layers are intentional.
public class CreateAseguradoraCommandValidator : AbstractValidator<CreateAseguradoraCommand>
{
    public CreateAseguradoraCommandValidator()
    {
        _ = RuleFor(v => v.NombreComercial)
            .NotEmpty().WithMessage("Commercial name is required.")
            .MaximumLength(150).WithMessage("Commercial name must not exceed 150 characters.");

        _ = RuleFor(v => v.CodigoIdentificadorNacional)
            .NotEmpty().WithMessage("National code is required.")
            .MaximumLength(50).WithMessage("National code must not exceed 50 characters.");
    }
}
