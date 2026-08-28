using FluentValidation;

namespace SOFIA.Application.Security.Commands.Login;

public class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        _ = RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("Username is required.")
            .MaximumLength(50).WithMessage("Username must not exceed 50 characters.");

        _ = RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.");
    }
}
