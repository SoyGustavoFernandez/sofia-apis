using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Commands.DeleteLote;

public class DeleteLoteInventarioHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteLoteInventarioCommand, Result>
{
    public async Task<Result> Handle(DeleteLoteInventarioCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.LotesInventario
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity is null)
        {
            return Result.Failure(Error.NotFound("LoteInventario.NotFound", "The specified batch does not exist."), 404);
        }

        var tieneStock = await context.LotesEnSucursal
            .AnyAsync(i => i.LoteId == request.Id && !i.IsDeleted, cancellationToken);
        var tieneVentas = await context.DetallesVenta
            .AnyAsync(d => d.LoteId == request.Id && !d.IsDeleted, cancellationToken);
        if (tieneStock || tieneVentas)
        {
            return Result.Failure(
                Error.Conflict("LoteInventario.InUse", "Cannot delete a batch that has stock or sales registered."),
                409);
        }

        _ = context.LotesInventario.Remove(entity);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
