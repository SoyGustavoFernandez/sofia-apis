using FluentValidation;

namespace SOFIA.Application.Empresas.Commands.ActivarSuscripcionEmpresa;

public class ActivarSuscripcionEmpresaCommandValidator : AbstractValidator<ActivarSuscripcionEmpresaCommand>
{
    public ActivarSuscripcionEmpresaCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();

        _ = RuleFor(x => x.FechaVencimiento)
            .Must(fecha => fecha > DateTimeOffset.UtcNow).WithMessage("The expiry date must be in the future.");
    }
}
