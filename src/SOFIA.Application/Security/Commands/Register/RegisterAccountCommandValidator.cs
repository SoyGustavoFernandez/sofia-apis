using FluentValidation;

namespace SOFIA.Application.Security.Commands.Register;

public class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountCommandValidator()
    {
        _ = RuleFor(x => x.EmpleadoId)
            .NotEmpty().WithMessage("Employee ID is required.");

        _ = RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

        _ = RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");
    }
}
