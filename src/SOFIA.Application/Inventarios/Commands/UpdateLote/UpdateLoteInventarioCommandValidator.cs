using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Commands.UpdateLote;

public class UpdateLoteInventarioValidator : AbstractValidator<UpdateLoteInventarioCommand>
{
    public UpdateLoteInventarioValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.NumeroLoteMfr)
            .NotEmpty().WithMessage("Batch number (Mfr) is required.")
            .MaximumLength(100).WithMessage("Batch number must not exceed 100 characters.");

        _ = RuleFor(v => v.FechaCaducidad)
            .NotEmpty().WithMessage("Expiration date is required.")
            .GreaterThan(DateTimeOffset.UtcNow).WithMessage("Expiration date must be in the future.");

        _ = RuleFor(v => v.FechaFabricacion)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow).WithMessage("Manufacture date cannot be in the future.")
            .When(v => v.FechaFabricacion.HasValue);
    }
}
