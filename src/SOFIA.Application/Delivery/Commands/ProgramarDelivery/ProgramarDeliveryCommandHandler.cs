using SOFIA.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Delivery.Commands.ProgramarDelivery;

public class ProgramarDeliveryCommandHandler(IApplicationDbContext dbContext, ISucursalAccess sucursalAccess) : IRequestHandler<ProgramarDeliveryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProgramarDeliveryCommand request, CancellationToken cancellationToken)
    {
        // Tenant-filtered lookup: a sale of another company is reported as missing
        var venta = await dbContext.Ventas
            .Where(v => v.Id == request.VentaId)
            .Select(v => new { v.SucursalId, v.Estado })
            .FirstOrDefaultAsync(cancellationToken);
        if (venta is null)
        {
            return Result.Failure<Guid>(Error.NotFound("Venta.NotFound", "La venta original no fue encontrada."), 404);
        }

        if (!await sucursalAccess.CanAccessAsync(venta.SucursalId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Forbidden("Delivery.Venta.SucursalNoPermitida", "You are not allowed to schedule deliveries for this branch's sales."), 403);
        }

        if (venta.Estado == EstadoVenta.Anulada)
        {
            return Result.Failure<Guid>(Error.Conflict("Delivery.Venta.Anulada", "A cancelled sale cannot be delivered."), 409);
        }

        if (await dbContext.DespachosDelivery.AnyAsync(d => d.VentaId == request.VentaId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.Conflict("Delivery.Venta.YaProgramado", "This sale already has a delivery dispatch."), 409);
        }

        var createResult = Domain.Entities.DespachoDelivery.Create(request.VentaId, request.PlataformaServicio, request.CodigoRastreo, request.DireccionEntrega, request.RepartidorNombre, request.EvidenciaFotograficaUrl);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.DespachosDelivery.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
