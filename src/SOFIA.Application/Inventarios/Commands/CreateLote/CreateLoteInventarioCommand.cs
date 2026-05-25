using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Inventarios.Commands.CreateLote;

public record CreateLoteInventarioCommand(
    Guid ProductoId,
    string NumeroLoteMfr,
    DateTimeOffset? FechaFabricacion,
    DateTimeOffset FechaCaducidad) : IRequest<Result<Guid>>;

public class CreateLoteInventarioValidator : AbstractValidator<CreateLoteInventarioCommand>
{
    public CreateLoteInventarioValidator()
    {
        _ = RuleFor(v => v.ProductoId)
            .NotEmpty().WithMessage("Producto ID is required.");

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

public class CreateLoteInventarioHandler(IApplicationDbContext context)
    : IRequestHandler<CreateLoteInventarioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLoteInventarioCommand request, CancellationToken cancellationToken)
    {
        // Validate product existence
        var productExists = await context.Medicamentos.AnyAsync(m => m.Id == request.ProductoId, cancellationToken);
        if (!productExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Medicamento.NotFound", "The specified product does not exist."));
        }

        // Validate batch number uniqueness
        var batchExists = await context.LotesInventario
            .AnyAsync(l => l.ProductoId == request.ProductoId && l.NumeroLoteMfr == request.NumeroLoteMfr, cancellationToken);

        if (batchExists)
        {
            return Result.Failure<Guid>(Error.Validation("LoteInventario.Duplicate", $"A batch with number '{request.NumeroLoteMfr}' already exists for this product."));
        }

        var result = LoteInventario.Create(
            request.ProductoId,
            request.NumeroLoteMfr,
            request.FechaFabricacion,
            request.FechaCaducidad);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.LotesInventario.Add(result.Value);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
