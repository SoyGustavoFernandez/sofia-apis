using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Commands.AperturarCaja;

public record AperturarCajaCommand(Guid SucursalId, Guid EmpleadoId, DateTime FechaHoraApertura, decimal MontoAperturaEfectivo) : IRequest<Result<Guid>>;

public class AperturarCajaCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<AperturarCajaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AperturarCajaCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Entities.POSSesionCaja.Create(request.SucursalId, request.EmpleadoId, request.FechaHoraApertura, request.MontoAperturaEfectivo);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.POSSesionesCaja.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
