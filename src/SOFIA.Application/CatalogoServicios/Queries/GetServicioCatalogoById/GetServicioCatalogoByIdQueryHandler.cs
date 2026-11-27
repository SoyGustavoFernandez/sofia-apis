using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.CatalogoServicios.Common;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.CatalogoServicios.Queries.GetServicioCatalogoById;

public class GetServicioCatalogoByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetServicioCatalogoByIdQuery, Result<ServicioCatalogoDto>>
{
    public async Task<Result<ServicioCatalogoDto>> Handle(GetServicioCatalogoByIdQuery request, CancellationToken cancellationToken)
    {
        var servicio = await context.ServiciosCatalogo
            .AsNoTracking()
            .Where(s => s.Id == request.Id && !s.IsDeleted)
            .Select(s => new ServicioCatalogoDto(s.Id, s.Nombre, s.Precio, s.AplicaMedicamento, s.RegistraResultado))
            .FirstOrDefaultAsync(cancellationToken);

        return servicio is null
            ? Result.Failure<ServicioCatalogoDto>(Error.NotFound("ServicioCatalogo.NotFound", "The service does not exist."), 404)
            : Result.Success(servicio);
    }
}
