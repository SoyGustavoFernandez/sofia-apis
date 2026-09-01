using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetRoles;

public record GetRolesQuery : IRequest<Result<PaginatedList<RolResponse>>>
{
    public string? NombreRol { get; init; }
    public string? Descripcion { get; init; }
    public int? NivelJerarquiaDesde { get; init; }
    public int? NivelJerarquiaHasta { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
