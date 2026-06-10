using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetRoles;

public record GetRolesQuery() : IRequest<Result<IReadOnlyList<RolResponse>>>;

public class GetRolesQueryHandler(IApplicationDbContext context) : IRequestHandler<GetRolesQuery, Result<IReadOnlyList<RolResponse>>>
{
    public async Task<Result<IReadOnlyList<RolResponse>>> Handle(GetRolesQuery request, CancellationToken cancellationToken)
    {
        var roles = await context.Roles
            .Where(r => !r.IsDeleted)
            .OrderByDescending(r => r.NivelJerarquia)
            .Select(r => new RolResponse(
                r.Id,
                r.NombreRol,
                r.Descripcion,
                r.NivelJerarquia,
                r.CreatedAt))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<RolResponse>>(roles);
    }
}
