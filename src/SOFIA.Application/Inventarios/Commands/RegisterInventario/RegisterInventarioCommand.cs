using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Inventarios.Commands.RegisterInventario;

public record RegisterInventarioCommand(
    Guid SucursalId,
    Guid LoteId,
    decimal Cantidad,
    bool EsAjusteDirecto = false) : ICommand<Guid>;

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

public class RegisterInventarioHandler(IApplicationDbContext context)
    : IRequestHandler<RegisterInventarioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(RegisterInventarioCommand request, CancellationToken cancellationToken)
    {
        // 1. Verify Sucursal existence
        var sucursalExists = await context.Sucursales.AnyAsync(s => s.Id == request.SucursalId, cancellationToken);
        if (!sucursalExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Sucursal.NotFound", "The specified branch does not exist."));
        }

        // 2. Verify Lote existence
        var loteExists = await context.LotesInventario.AnyAsync(l => l.Id == request.LoteId, cancellationToken);
        if (!loteExists)
        {
            return Result.Failure<Guid>(Error.NotFound("LoteInventario.NotFound", "The specified batch does not exist."));
        }

        // 3. Check if already exists in this branch
        var existingEntry = await context.LotesEnSucursal
            .FirstOrDefaultAsync(x => x.SucursalId == request.SucursalId && x.LoteId == request.LoteId, cancellationToken);

        if (existingEntry != null)
        {
            if (request.EsAjusteDirecto)
            {
                existingEntry.UpdateStock(request.Cantidad);
            }
            else
            {
                existingEntry.AddStock(request.Cantidad);
            }

            _ = await context.SaveChangesAsync(cancellationToken);
            return Result.Success(existingEntry.Id);
        }

        // 4. Create new entry
        var result = InventarioSucursal.Create(request.SucursalId, request.LoteId, request.Cantidad);
        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.LotesEnSucursal.Add(result.Value);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
