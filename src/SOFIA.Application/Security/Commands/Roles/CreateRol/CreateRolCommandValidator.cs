using FluentValidation;

namespace SOFIA.Application.Security.Commands.Roles.CreateRol;

public class CreateRolCommandValidator : AbstractValidator<CreateRolCommand>
{
    public CreateRolCommandValidator()
    {
        _ = RuleFor(x => x.NombreRol)
            .NotEmpty().WithMessage("El nombre del rol es obligatorio.")
            .MaximumLength(50).WithMessage("El nombre del rol no puede exceder los 50 caracteres.");

        _ = RuleFor(x => x.Descripcion)
            .MaximumLength(255).WithMessage("La descripción no puede exceder los 255 caracteres.");

        _ = RuleFor(x => x.NivelJerarquia)
            .GreaterThanOrEqualTo(0).WithMessage("El nivel de jerarquía debe ser mayor o igual a 0.");
    }
}
