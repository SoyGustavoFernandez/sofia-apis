using FluentValidation;

namespace SOFIA.Application.Security.Commands.Logout;

public class LogoutCommandValidator : AbstractValidator<LogoutCommand>
{
    public LogoutCommandValidator() => _ = RuleFor(x => x.CuentaId)
            .NotEmpty().WithMessage("El ID de la cuenta es obligatorio.");
}
