using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;

public class AislarLoteCuarentenaCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<AislarLoteCuarentenaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AislarLoteCuarentenaCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Entities.DigemidInventarioCuarentena.Create(request.SucursalId, request.LoteId, request.DetalleDevId, request.CantidadAislada, request.MotivoAislamiento, request.EstadoResolucion, request.EmpleadoRegistraId, request.FechaIngresoCuarentena);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.DigemidInventarioCuarentena.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
