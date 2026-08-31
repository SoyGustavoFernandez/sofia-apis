using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Transferencias.Commands.CreateTransferencia;

public class CreateTransferenciaCommandValidator : AbstractValidator<CreateTransferenciaCommand>
{
    public CreateTransferenciaCommandValidator()
    {
        _ = RuleFor(v => v.SucursalDestinoId)
            .NotEmpty().WithMessage("Destination branch is required.");

        _ = RuleFor(v => v.Detalles)
            .NotEmpty().WithMessage("Transfer must contain at least one detail.");

        _ = RuleForEach(v => v.Detalles).ChildRules(detail =>
        {
            _ = detail.RuleFor(d => d.LoteId).NotEmpty().WithMessage("El lote ID es requerido.");
            _ = detail.RuleFor(d => d.CantidadEnviada).GreaterThan(0).WithMessage("Sent quantity must be greater than zero.");
        });
    }
}
