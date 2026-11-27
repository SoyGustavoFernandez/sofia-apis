using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Citas.Commands.ReprogramarCita;

public class ReprogramarCitaCommandHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<ReprogramarCitaCommand, Result>
{
    public async Task<Result> Handle(ReprogramarCitaCommand request, CancellationToken cancellationToken)
    {
        var sucursal = currentUser.GetSucursalId();
        if (sucursal.IsFailure)
        {
            return Result.Failure(sucursal.Error);
        }

        var cita = await context.CitasServicio.FirstOrDefaultAsync(c => c.Id == request.Id && c.SucursalId == sucursal.Value, cancellationToken);
        if (cita is null)
        {
            return Result.Failure(Error.NotFound("CitaServicio.NotFound", "The appointment does not exist."), 404);
        }

        if (!await context.ServiciosCatalogo.AnyAsync(s => s.Id == request.ServicioId, cancellationToken))
        {
            return Result.Failure(Error.NotFound("ServicioCatalogo.NotFound", "The service does not exist."), 404);
        }

        var result = cita.Reprogramar(request.ServicioId, request.FechaHora, request.AceptaRecordatorio, DateTimeOffset.UtcNow);
        if (result.IsFailure)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
