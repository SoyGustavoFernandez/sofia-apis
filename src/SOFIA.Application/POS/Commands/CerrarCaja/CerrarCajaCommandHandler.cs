using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Commands.CerrarCaja;

public class CerrarCajaCommandHandler(IApplicationDbContext dbContext) : IRequestHandler<CerrarCajaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CerrarCajaCommand request, CancellationToken cancellationToken)
    {
        var sesion = await dbContext.POSSesionesCaja.FirstOrDefaultAsync(x => x.Id == request.SesionId, cancellationToken);
        if (sesion == null)
        {
            return Result.Failure<Guid>(Error.NotFound("Caja", "Sesion no encontrada"));
        }

        var result = sesion.Cerrar(DateTime.UtcNow, request.MontoCierre, request.MontoCierre);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = await dbContext.SaveChangesAsync(cancellationToken);
        return Result.Success(sesion.Id);
    }
}
