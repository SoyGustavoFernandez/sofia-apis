using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.CatalogoServicios.Common;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.CatalogoServicios.Queries.GetServiciosCatalogo;

public class GetServiciosCatalogoQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetServiciosCatalogoQuery, Result<PaginatedList<ServicioCatalogoDto>>>
{
    public async Task<Result<PaginatedList<ServicioCatalogoDto>>> Handle(GetServiciosCatalogoQuery request, CancellationToken cancellationToken)
    {
        var query = context.ServiciosCatalogo.AsNoTracking().Where(s => !s.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Nombre))
        {
            query = query.Where(s => s.Nombre.Contains(request.Nombre));
        }

        if (request.PrecioMin is { } min)
        {
            query = query.Where(s => s.Precio >= min);
        }

        if (request.PrecioMax is { } max)
        {
            query = query.Where(s => s.Precio <= max);
        }

        var pagina = await PaginatedList<ServicioCatalogoDto>.CreateAsync(
            query.OrderBy(s => s.Nombre).ThenBy(s => s.Id).Select(s => new ServicioCatalogoDto(s.Id, s.Nombre, s.Precio, s.AplicaMedicamento, s.RegistraResultado)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(pagina);
    }
}
