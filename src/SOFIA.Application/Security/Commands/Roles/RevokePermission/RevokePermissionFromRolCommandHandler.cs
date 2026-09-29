using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.RevokePermission;

public class RevokePermissionFromRolCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IPermissionCache permissionCache) : IRequestHandler<RevokePermissionFromRolCommand, Result>
{
    public async Task<Result> Handle(RevokePermissionFromRolCommand request, CancellationToken cancellationToken)
    {
        var permiso = await context.PermisosRol
            .FirstOrDefaultAsync(p => p.Id == request.PermisoId && !p.IsDeleted, cancellationToken);

        if (permiso is null)
        {
            return Result.Failure(Error.NotFound("Permiso.NotFound", "El permiso especificado no existe."));
        }

        var nombreRol = await context.Roles
            .Where(r => r.Id == permiso.RolId)
            .Select(r => r.NombreRol)
            .FirstOrDefaultAsync(cancellationToken);

        permiso.IsDeleted = true;
        permiso.DeletedAt = DateTimeOffset.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        if (nombreRol is not null)
        {
            permissionCache.InvalidateForCurrentEmpresa(currentUser, nombreRol);
        }

        return Result.Success();
    }
}
