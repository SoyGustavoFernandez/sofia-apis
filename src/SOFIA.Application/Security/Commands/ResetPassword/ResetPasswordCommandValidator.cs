using FluentValidation;

namespace SOFIA.Application.Security.Commands.ResetPassword;

public class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        _ = RuleFor(x => x.NombreUsuario)
            .NotEmpty().WithMessage("Username is required.");

        _ = RuleFor(x => x.Token)
            .NotEmpty().WithMessage("Recovery token is required.");

        _ = RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.");
    }
}
