using FluentValidation;

namespace SOFIA.Application.Security.Commands.Cuentas.UpdateCuenta;

public class UpdateCuentaCommandValidator : AbstractValidator<UpdateCuentaCommand>
{
    public UpdateCuentaCommandValidator() => _ = RuleFor(x => x.Id).NotEmpty();
}
