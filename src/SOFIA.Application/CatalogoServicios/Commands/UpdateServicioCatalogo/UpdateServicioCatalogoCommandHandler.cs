using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.CatalogoServicios.Common;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.CatalogoServicios.Commands.UpdateServicioCatalogo;

public class UpdateServicioCatalogoCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateServicioCatalogoCommand, Result>
{
    public async Task<Result> Handle(UpdateServicioCatalogoCommand request, CancellationToken cancellationToken)
    {
        var servicio = await context.ServiciosCatalogo.FirstOrDefaultAsync(s => s.Id == request.Id, cancellationToken);
        if (servicio is null)
        {
            return Result.Failure(Error.NotFound("ServicioCatalogo.NotFound", "The service does not exist."), 404);
        }

        var duplicado = await ServicioCatalogoDuplicateChecker.FindAsync(context, servicio.Id, request.Nombre.Trim(), cancellationToken);
        if (duplicado is not null)
        {
            return Result.Failure(duplicado, 409);
        }

        var result = servicio.Update(request.Nombre, request.Precio, request.AplicaMedicamento, request.RegistraResultado);
        if (result.IsFailure)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
