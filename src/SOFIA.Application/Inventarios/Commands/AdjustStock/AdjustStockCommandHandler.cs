using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Commands.AdjustStock;

public class AdjustStockCommandHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess)
    : IRequestHandler<AdjustStockCommand, Result>
{
    public async Task<Result> Handle(AdjustStockCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.LotesEnSucursal
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity is null)
        {
            return Result.Failure(Error.NotFound("InventarioSucursal.NotFound", "The specified stock record does not exist."), 404);
        }

        if (!await sucursalAccess.CanAccessAsync(entity.SucursalId, cancellationToken))
        {
            return Result.Failure(Error.Forbidden("Inventario.Sucursal.NoPermitida", "You are not allowed to operate on this branch's stock."), 403);
        }

        var result = entity.AdjustStock(request.NuevaCantidad);
        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
