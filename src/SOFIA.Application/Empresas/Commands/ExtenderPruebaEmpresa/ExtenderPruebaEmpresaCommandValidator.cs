using FluentValidation;

namespace SOFIA.Application.Empresas.Commands.ExtenderPruebaEmpresa;

public class ExtenderPruebaEmpresaCommandValidator : AbstractValidator<ExtenderPruebaEmpresaCommand>
{
    public ExtenderPruebaEmpresaCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();

        _ = RuleFor(x => x.FechaVencimiento)
            .Must(fecha => fecha > DateTimeOffset.UtcNow).WithMessage("The expiry date must be in the future.");
    }
}
