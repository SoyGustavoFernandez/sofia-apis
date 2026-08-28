using FluentValidation;

namespace SOFIA.Application.Security.Commands.Roles.UpdateRol;

public class UpdateRolCommandValidator : AbstractValidator<UpdateRolCommand>
{
    public UpdateRolCommandValidator()
    {
        _ = RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Role ID is required.");

        _ = RuleFor(x => x.Descripcion)
            .MaximumLength(255).WithMessage("Description must not exceed 255 characters.");

        _ = RuleFor(x => x.NivelJerarquia)
            .GreaterThanOrEqualTo(0).WithMessage("Hierarchy level must be greater than or equal to 0.");
    }
}
