using FluentValidation;
using SOFIA.Domain.Entities;

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

        // Fixed property name so the error code does not depend on the payment index
        _ = RuleFor(v => v.Pagos)
            .Must(pagos => pagos.TrueForAll(p => !VentaPago.RequiereReferencia(p.MetodoPago) || !string.IsNullOrWhiteSpace(p.ReferenciaOperacion)))
            .WithMessage("An operation reference is required for non-cash payments.")
            .OverridePropertyName("Pagos.ReferenciaRequerida")
            .When(v => v.Pagos is not null);

        _ = RuleFor(v => v.MontoCubiertoSeguro)
            .GreaterThanOrEqualTo(0).WithMessage("The insurance coverage cannot be negative.")
            .When(v => v.MontoCubiertoSeguro.HasValue);

        _ = RuleFor(v => v.AseguradoraId)
            .NotEmpty().WithMessage("An insurer is required when the sale has insurance coverage.")
            .When(v => v.MontoCubiertoSeguro > 0);
    }
}
