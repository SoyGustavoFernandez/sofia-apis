using FluentValidation;

namespace SOFIA.Application.Security.Commands.ChangePassword;

public class ChangePasswordCommandValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordCommandValidator()
    {
        _ = RuleFor(x => x.CuentaId)
            .NotEmpty().WithMessage("Account ID is required.");

        _ = RuleFor(x => x.CurrentPassword)
            .NotEmpty().WithMessage("Current password is required.");

        // Same rules as account creation and password reset
        _ = RuleFor(x => x.NewPassword)
            .NotEmpty().WithMessage("New password is required.")
            .MinimumLength(8).WithMessage("New password must be at least 8 characters long.")
            .NotEqual(x => x.CurrentPassword, StringComparer.Ordinal).WithMessage("New password must be different from the current password.");
    }
}
