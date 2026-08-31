using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Commands.UpdateLote;

public class UpdateLoteInventarioHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateLoteInventarioCommand, Result>
{
    public async Task<Result> Handle(UpdateLoteInventarioCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.LotesInventario
            .FindAsync([request.Id], cancellationToken);

        if (entity is null)
        {
            return Result.Failure(Error.NotFound("LoteInventario.NotFound", "The specified batch does not exist."), 404);
        }

        // Validate batch number uniqueness (excluding current record)
        var batchExists = await context.LotesInventario
            .AnyAsync(l => l.ProductoId == entity.ProductoId &&
                           l.NumeroLoteMfr == request.NumeroLoteMfr &&
                           l.Id != request.Id, cancellationToken);

        if (batchExists)
        {
            return Result.Failure(Error.Validation("LoteInventario.Duplicate", $"A batch with number '{request.NumeroLoteMfr}' already exists for this product."));
        }

        var result = entity.Update(
            request.NumeroLoteMfr,
            request.FechaFabricacion,
            request.FechaCaducidad);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
