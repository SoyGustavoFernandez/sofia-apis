using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.PresentacionesVenta.Commands.DeletePresentacionVenta;

public class DeletePresentacionVentaCommandHandler(IApplicationDbContext context) : IRequestHandler<DeletePresentacionVentaCommand, Result>
{
    public async Task<Result> Handle(DeletePresentacionVentaCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.PresentacionesVenta
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("PresentacionVenta.NotFound", $"Presentacion de Venta with ID {request.Id} was not found."), 404);
        }

        var referenciadaPorVentas = await context.DetallesVenta
            .AnyAsync(d => d.PresentacionVentaId == request.Id && !d.IsDeleted, cancellationToken);
        if (referenciadaPorVentas)
        {
            return Result.Failure(
                Error.Conflict("PresentacionVenta.InUse", "Cannot delete a sale presentation that has sales registered."),
                409);
        }

        _ = context.PresentacionesVenta.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }
}
