using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.GenerarActaDestruccion;

public class GenerarActaDestruccionCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<GenerarActaDestruccionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GenerarActaDestruccionCommand request, CancellationToken cancellationToken)
    {
        // The regent may differ from who records the act, but must belong to the same company
        if (!await dbContext.Empleados.AnyAsync(e => e.Id == request.RegenteResponsableId, cancellationToken))
        {
            return Result.Failure<Guid>(Error.NotFound("Empleado.NotFound", "El empleado especificado no existe."), 404);
        }

        var createResult = Domain.Entities.DigemidActaDestruccion.Create(request.NumeroResolucionInterna, request.EmpresaResiduosBiocontaminados, request.ManifiestoTransporteDoc, request.FechaEjecucion, request.RegenteResponsableId, request.RutaActaFirmadaPdf);
        if (createResult.IsFailure)
        {
            return Result.Failure<Guid>(createResult.Error);
        }

        var entity = createResult.Value!;
        _ = dbContext.DIGEMIDActasDestruccion.Add(entity);
        _ = await dbContext.SaveChangesAsync(cancellationToken);

        return Result.Success(entity.Id);

    }
}
