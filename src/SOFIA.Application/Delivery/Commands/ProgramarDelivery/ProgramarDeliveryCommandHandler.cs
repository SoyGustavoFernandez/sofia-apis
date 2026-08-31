using SOFIA.Domain.Enums;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Delivery.Commands.ProgramarDelivery;

public class ProgramarDeliveryCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<ProgramarDeliveryCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(ProgramarDeliveryCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Entities.DespachoDelivery.Create(request.VentaId, request.PlataformaServicio, request.CodigoRastreo, request.EstadoDespacho, request.DireccionEntrega, request.RepartidorNombre, request.EvidenciaFotograficaUrl);
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
