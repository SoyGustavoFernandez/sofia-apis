using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.SeriesFiscales.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.SeriesFiscales.Commands.CreateSerieFiscal;

public class CreateSerieFiscalCommandHandler(IApplicationDbContext context)
    : IRequestHandler<CreateSerieFiscalCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateSerieFiscalCommand request, CancellationToken cancellationToken)
    {
        var conflictResult = await SerieFiscalConflictChecker.CheckAsync(
            context, null, request.SucursalId, request.TipoComprobante, request.PrefijoSerie, request.EstadoSerie, cancellationToken);
        if (conflictResult.IsFailure)
        {
            return Result.Failure<Guid>(conflictResult.Error, conflictResult.StatusCode);
        }

        var serieResult = SunatSerieFiscal.Create(
            request.SucursalId, request.TipoComprobante, request.PrefijoSerie, request.CorrelativoActual, request.EstadoSerie);
        if (serieResult.IsFailure)
        {
            return Result.Failure<Guid>(serieResult.Error);
        }

        _ = context.SUNATSeriesFiscales.Add(serieResult.Value!);

        try
        {
            _ = await context.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateException ex) when (SerieFiscalConflictChecker.MapUniqueViolation(ex) is { } error)
        {
            return Result.Failure<Guid>(error, 409);
        }

        return Result.Success(serieResult.Value!.Id, 201);
    }
}
