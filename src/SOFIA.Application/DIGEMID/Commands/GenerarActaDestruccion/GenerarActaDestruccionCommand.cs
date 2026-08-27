using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.GenerarActaDestruccion;

public record GenerarActaDestruccionCommand(string NumeroResolucionInterna, string EmpresaResiduosBiocontaminados, string? ManifiestoTransporteDoc, DateTime FechaEjecucion, Guid RegenteResponsableId, string? RutaActaFirmadaPdf) : ICommand<Guid>;

public class GenerarActaDestruccionCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<GenerarActaDestruccionCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(GenerarActaDestruccionCommand request, CancellationToken cancellationToken)
    {
        var createResult = Domain.Entities.DIGEMIDActaDestruccion.Create(request.NumeroResolucionInterna, request.EmpresaResiduosBiocontaminados, request.ManifiestoTransporteDoc, request.FechaEjecucion, request.RegenteResponsableId, request.RutaActaFirmadaPdf);
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
