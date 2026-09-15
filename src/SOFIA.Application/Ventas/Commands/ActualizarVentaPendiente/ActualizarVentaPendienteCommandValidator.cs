using FluentValidation;

namespace SOFIA.Application.Ventas.Commands.ActualizarVentaPendiente;

public class ActualizarVentaPendienteCommandValidator : AbstractValidator<ActualizarVentaPendienteCommand>
{
    public ActualizarVentaPendienteCommandValidator()
    {
        _ = RuleFor(v => v.VentaId).NotEmpty();

        _ = RuleFor(v => v.Detalles)
            .NotEmpty().WithMessage("A sale must have at least one detail.");

        _ = RuleForEach(v => v.Detalles).ChildRules(detail =>
        {
            _ = detail.RuleFor(d => d.LoteId).NotEmpty();
            _ = detail.RuleFor(d => d.Cantidad).GreaterThan(0);
            _ = detail.RuleFor(d => d.PrecioUnitario).GreaterThanOrEqualTo(0);
        });
    }
}
