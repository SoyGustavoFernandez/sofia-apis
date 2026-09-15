using FluentValidation;

namespace SOFIA.Application.Ventas.Commands.CompletarVenta;

public class CompletarVentaCommandValidator : AbstractValidator<CompletarVentaCommand>
{
    public CompletarVentaCommandValidator()
    {
        _ = RuleFor(v => v.VentaId).NotEmpty();

        _ = RuleFor(v => v.Pagos)
            .NotEmpty().WithMessage("A sale must have at least one payment.");

        _ = RuleForEach(v => v.Pagos).ChildRules(pago =>
        {
            _ = pago.RuleFor(p => p.MetodoPago).IsInEnum();
            _ = pago.RuleFor(p => p.MontoPagado).GreaterThan(0);
            _ = pago.RuleFor(p => p.ReferenciaOperacion).MaximumLength(100);
        });
    }
}
