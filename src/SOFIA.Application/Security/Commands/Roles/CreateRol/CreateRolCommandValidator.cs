using FluentValidation;

namespace SOFIA.Application.Security.Commands.Roles.CreateRol;

public class CreateRolCommandValidator : AbstractValidator<CreateRolCommand>
{
    public CreateRolCommandValidator()
    {
        _ = RuleFor(x => x.NombreRol)
            .NotEmpty().WithMessage("Role name is required.")
            .MaximumLength(50).WithMessage("Role name must not exceed 50 characters.");

        _ = RuleFor(x => x.Descripcion)
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters.");

        _ = RuleFor(x => x.NivelJerarquia)
            .GreaterThanOrEqualTo(0).WithMessage("Hierarchy level must be greater than or equal to 0.");
    }
}
