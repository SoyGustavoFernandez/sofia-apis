using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.DTOs;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetPermissions;

public record GetPermissionsByRolQuery(Guid RolId) : IRequest<Result<IReadOnlyList<PermisoResponse>>>;

public class GetPermissionsByRolQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPermissionsByRolQuery, Result<IReadOnlyList<PermisoResponse>>>
{
    public async Task<Result<IReadOnlyList<PermisoResponse>>> Handle(GetPermissionsByRolQuery request, CancellationToken cancellationToken)
    {
        var rolExists = await context.Roles.AnyAsync(r => r.Id == request.RolId && !r.IsDeleted, cancellationToken);
        if (!rolExists)
        {
            return Result.Failure<IReadOnlyList<PermisoResponse>>(Error.NotFound("Rol.NotFound", "El rol especificado no existe."));
        }

        var permisos = await context.PermisosRol
            .Where(p => p.RolId == request.RolId && !p.IsDeleted)
            .Select(p => new PermisoResponse(p.Id, p.ModuloSistema, p.Accion))
            .ToListAsync(cancellationToken);

        return Result.Success<IReadOnlyList<PermisoResponse>>(permisos);
    }
}
