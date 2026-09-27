using FluentValidation;

namespace SOFIA.Application.POS.Commands.AperturarCaja;

public class AperturarCajaCommandValidator : AbstractValidator<AperturarCajaCommand>
{
    public AperturarCajaCommandValidator() => _ = RuleFor(x => x.MontoAperturaEfectivo)
            .GreaterThanOrEqualTo(0).WithMessage("Opening cash amount must be greater than or equal to 0.");
}
