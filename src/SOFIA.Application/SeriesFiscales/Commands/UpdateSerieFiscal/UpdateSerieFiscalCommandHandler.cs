using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.SeriesFiscales.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.SeriesFiscales.Commands.UpdateSerieFiscal;

public class UpdateSerieFiscalCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateSerieFiscalCommand, Result>
{
    public async Task<Result> Handle(UpdateSerieFiscalCommand request, CancellationToken cancellationToken)
    {
        var serie = await context.SUNATSeriesFiscales
            .FirstOrDefaultAsync(s => s.Id == request.Id && !s.IsDeleted, cancellationToken);

        if (serie is null)
        {
            return Result.Failure(Error.NotFound("SerieFiscal.NotFound", "The specified fiscal series does not exist."), 404);
        }

        var tieneComprobantes = await context.SUNATComprobantesEmitidos
            .AnyAsync(c => c.SerieId == request.Id && !c.IsDeleted, cancellationToken);
        if (tieneComprobantes && serie.CambiaNumeracion(request.SucursalId, request.TipoComprobante, request.PrefijoSerie, request.CorrelativoActual))
        {
            return Result.Failure(Error.Conflict("SerieFiscal.ConComprobantes", "Only the status of a series that already issued documents can change."), 409);
        }

        var conflictResult = await SerieFiscalConflictChecker.CheckAsync(
            context, request.Id, request.SucursalId, request.TipoComprobante, request.PrefijoSerie, request.EstadoSerie, cancellationToken);
        if (conflictResult.IsFailure)
        {
            return conflictResult;
        }

        var updateResult = serie.Update(
            request.SucursalId, request.TipoComprobante, request.PrefijoSerie, request.CorrelativoActual, request.EstadoSerie);
        if (updateResult.IsFailure)
        {
            return updateResult;
        }

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SerieFiscalConflictChecker.MapUniqueViolation(ex) is { } error)
        {
            return Result.Failure(error, 409);
        }

        return Result.Success();
    }
}
