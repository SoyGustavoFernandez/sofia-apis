using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Citas.Commands.CerrarCita;

public class CerrarCitaCommandHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<CerrarCitaCommand, Result>
{
    public async Task<Result> Handle(CerrarCitaCommand request, CancellationToken cancellationToken)
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

        var result = cita.Cerrar(request.Estado);
        if (result.IsFailure)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
