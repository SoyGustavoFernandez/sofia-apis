using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetRoles;

public class GetRolesQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRolesQuery, Result<PaginatedList<RolResponse>>>
{
    public async Task<Result<PaginatedList<RolResponse>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var query = context.Roles
            .Where(r => !r.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.NombreRol))
        {
            query = query.Where(r => r.NombreRol.Contains(request.NombreRol));
        }

        if (!string.IsNullOrWhiteSpace(request.Descripcion))
        {
            query = query.Where(r => r.Descripcion != null && r.Descripcion.Contains(request.Descripcion));
        }

        if (request.NivelJerarquiaDesde.HasValue)
        {
            query = query.Where(r => r.NivelJerarquia >= request.NivelJerarquiaDesde.Value);
        }

        if (request.NivelJerarquiaHasta.HasValue)
        {
            query = query.Where(r => r.NivelJerarquia <= request.NivelJerarquiaHasta.Value);
        }

        var projected = query
            .OrderByDescending(r => r.NivelJerarquia)
            .Select(r => new RolResponse(r.Id, r.NombreRol, r.Descripcion, r.NivelJerarquia, r.CreatedAt));

        var paginated = await PaginatedList<RolResponse>.CreateAsync(projected, request.PageNumber, request.PageSize);

        return Result.Success(paginated);
    }
}
