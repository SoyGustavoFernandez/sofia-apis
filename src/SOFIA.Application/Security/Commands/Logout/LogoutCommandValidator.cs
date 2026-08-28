using FluentValidation;

namespace SOFIA.Application.Security.Commands.Logout;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator() => _ = RuleFor(x => x.CuentaId)
            .NotEmpty().WithMessage("Account ID is required.");
}
