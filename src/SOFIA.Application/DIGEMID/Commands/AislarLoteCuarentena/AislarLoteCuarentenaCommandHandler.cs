using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;

public class AislarLoteCuarentenaCommandHandler(IApplicationDbContext dbContext, ICurrentUser currentUser) : IRequestHandler<AislarLoteCuarentenaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AislarLoteCuarentenaCommand request, CancellationToken cancellationToken)
    {
        // DIGEMID record: who isolated the batch and where comes from the session, never from the request
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<Guid>(sucursalResult.Error);
        }

        var empleadoResult = currentUser.GetEmpleadoId();
        if (empleadoResult.IsFailure)
        {
            return Result.Failure<Guid>(empleadoResult.Error);
        }

        // Tenant-filtered lookups: an id from another company must not be stored as a reference
        if (!await dbContext.LotesInventario.AnyAsync(l => l.Id == request.LoteId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("LoteInventario.NotFound", "The specified batch does not exist."), 404);
        }

        if (request.DetalleDevId is { } detalleDevId
            && !await dbContext.DetallesDevolucion.AnyAsync(d => d.Id == detalleDevId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("DetalleDevolucion.NotFound", "The specified return line does not exist."), 404);
        }

        var createResult = Domain.Entities.DigemidInventarioCuarentena.Create(sucursalResult.Value, request.LoteId, request.DetalleDevId, request.CantidadAislada, request.MotivoAislamiento, request.EstadoResolucion, empleadoResult.Value);
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
