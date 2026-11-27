using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using System.Text.Json;

namespace SOFIA.Application.Ventas.Commands.CreateVenta;

public class CreateVentaCommandValidator : AbstractValidator<CreateVentaCommand>
{
    public CreateVentaCommandValidator()
    {
        _ = RuleFor(v => v.Detalles)
            .NotEmpty().WithMessage("A sale must have at least one detail.")
            .When(v => v.Atencion is null);

        _ = When(v => v.Atencion is not null, () =>
        {
            _ = RuleFor(v => v.Atencion!.ServicioId).NotEmpty().OverridePropertyName("Atencion.ServicioId");
            _ = RuleFor(v => v.Atencion!.Origen).IsInEnum().OverridePropertyName("Atencion.Origen");
            _ = RuleFor(v => v.Atencion!.MedicamentoTraido).MaximumLength(200).OverridePropertyName("Atencion.MedicamentoTraido");
            _ = RuleFor(v => v.Atencion!.Dosis).MaximumLength(200).OverridePropertyName("Atencion.Dosis");
            _ = RuleFor(v => v.Atencion!.Resultado).MaximumLength(200).OverridePropertyName("Atencion.Resultado");

            // A parked sale would leave the service neither charged nor pending, and a claim needs a product line
            _ = RuleFor(v => v.Estado).NotEqual(EstadoVenta.Pendiente).OverridePropertyName("Atencion.NoPendiente");
            _ = RuleFor(v => v.AseguradoraId).Null().OverridePropertyName("Atencion.SinSeguro");
        });

        _ = RuleForEach(v => v.Detalles).ChildRules(detail =>
        {
            _ = detail.RuleFor(d => d.LoteId).NotEmpty();
            _ = detail.RuleFor(d => d.Cantidad).GreaterThan(0);
        });

        _ = RuleFor(v => v.Pagos)
            .NotEmpty().WithMessage("A sale must have at least one payment.")
            .When(v => v.Estado != EstadoVenta.Pendiente);

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
