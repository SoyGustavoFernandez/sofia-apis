using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Transferencias.Commands.RecibirTransferencia;

public class RecibirTransferenciaCommandValidator : AbstractValidator<RecibirTransferenciaCommand>
{
    public RecibirTransferenciaCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty().WithMessage("Transfer ID is required.");
        _ = RuleFor(v => v.Recepciones).NotEmpty().WithMessage("At least one lot reception must be provided.");
        _ = RuleForEach(v => v.Recepciones).ChildRules(detail =>
        {
            _ = detail.RuleFor(d => d.LoteId).NotEmpty().WithMessage("El lote ID es requerido.");
            _ = detail.RuleFor(d => d.CantidadRecibida).GreaterThanOrEqualTo(0).WithMessage("Received quantity cannot be negative.");
        });
    }
}
