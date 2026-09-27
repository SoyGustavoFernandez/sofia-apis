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
            .NotEmpty().WithMessage("A sale must have at least one detail.");

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

        _ = RuleFor(v => v.MontoCubiertoSeguro)
            .GreaterThanOrEqualTo(0).WithMessage("The insurance coverage cannot be negative.")
            .When(v => v.MontoCubiertoSeguro.HasValue);

        _ = RuleFor(v => v.AseguradoraId)
            .NotEmpty().WithMessage("An insurer is required when the sale has insurance coverage.")
            .When(v => v.MontoCubiertoSeguro > 0);
    }
}
