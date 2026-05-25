using FluentValidation;

namespace SOFIA.Application.Security.Commands.Roles.UpdateRol;

public class UpdateRolCommandValidator : AbstractValidator<UpdateRolCommand>
{
    public UpdateRolCommandValidator()
    {
        _ = RuleFor(x => x.Id)
            .NotEmpty().WithMessage("El ID del rol es obligatorio.");

        _ = RuleFor(x => x.Descripcion)
            .MaximumLength(255).WithMessage("La descripción no puede exceder los 255 caracteres.");

        _ = RuleFor(x => x.NivelJerarquia)
            .GreaterThanOrEqualTo(0).WithMessage("El nivel de jerarquía debe ser mayor o igual a 0.");
    }
}
