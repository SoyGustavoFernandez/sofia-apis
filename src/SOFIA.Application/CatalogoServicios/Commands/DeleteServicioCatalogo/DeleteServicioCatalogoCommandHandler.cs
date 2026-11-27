using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.CatalogoServicios.Commands.DeleteServicioCatalogo;

public class DeleteServicioCatalogoCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteServicioCatalogoCommand, Result>
{
    public async Task<Result> Handle(DeleteServicioCatalogoCommand request, CancellationToken cancellationToken)
    {
        var servicio = await context.ServiciosCatalogo.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (servicio is null)
        {
            return Result.Failure(Error.NotFound("ServicioCatalogo.NotFound", "The service does not exist."), 404);
        }

        if (await context.CitasServicio.AnyAsync(c => c.ServicioId == servicio.Id, cancellationToken))
        {
            return Result.Failure(Error.Conflict("ServicioCatalogo.EnUso", "The service has appointments."), 409);
        }

        // Soft delete is applied by the DbContext
        _ = context.ServiciosCatalogo.Remove(servicio);
        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
