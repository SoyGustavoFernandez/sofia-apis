using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Inventarios.Commands.RegisterInventario;

public class RegisterInventarioValidator : AbstractValidator<RegisterInventarioCommand>
{
    public RegisterInventarioValidator()
    {
        _ = RuleFor(v => v.SucursalId)
            .NotEmpty().WithMessage("Sucursal ID is required.");

        _ = RuleFor(v => v.LoteId)
            .NotEmpty().WithMessage("Lote ID is required.");

        _ = RuleFor(v => v.Cantidad)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
    }
}
