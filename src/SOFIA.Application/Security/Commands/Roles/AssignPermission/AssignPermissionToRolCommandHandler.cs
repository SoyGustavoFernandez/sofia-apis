using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.AssignPermission;

public class AssignPermissionToRolCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IPermissionCache permissionCache) : IRequestHandler<AssignPermissionToRolCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AssignPermissionToRolCommand request, CancellationToken cancellationToken)
    {
        var nombreRol = await context.Roles
            .Where(r => r.Id == request.RolId && !r.IsDeleted)
            .Select(r => r.NombreRol)
            .FirstOrDefaultAsync(cancellationToken);

        if (nombreRol is null)
        {
            return Result.Failure<Guid>(Error.NotFound("Rol.NotFound", "El rol especificado no existe."), 404);
        }

        // Granting permissions to a role you hold is self-promotion; only an Admin may do it
        if (currentUser.IsInRole(nombreRol) && !currentUser.IsInRole(Rol.AdminRoleName))
        {
            return Result.Failure<Guid>(Error.Forbidden("Rol.PermisosPropios", "No puedes ampliar los permisos de un rol que tienes asignado."), 403);
        }

        // Check including soft-deleted rows to avoid unique constraint violations on restore
        var existing = await context.PermisosRol
            .IgnoreQueryFilters([QueryFilters.SoftDelete])
            .FirstOrDefaultAsync(p => p.RolId == request.RolId &&
                                      p.ModuloSistema == request.ModuloSistema &&
                                      p.Accion == request.Accion, cancellationToken);

        if (existing is not null)
        {
            if (!existing.IsDeleted)
            {
                return Result.Failure<Guid>(Error.Conflict("Permiso.Duplicate", "This permission is already assigned to this role."));
            }

            // Restore a previously revoked permission instead of inserting a duplicate
            existing.IsDeleted = false;
            existing.DeletedAt = null;
            existing.DeletedBy = null;
            _ = await context.SaveChangesAsync(cancellationToken);
            permissionCache.InvalidateForCurrentEmpresa(currentUser, nombreRol);
            return Result.Success(existing.Id);
        }

        var result = PermisoRol.Create(request.RolId, request.ModuloSistema, request.Accion);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.PermisosRol.Add(result.Value!);
        _ = await context.SaveChangesAsync(cancellationToken);
        permissionCache.InvalidateForCurrentEmpresa(currentUser, nombreRol);

        return Result.Success(result.Value!.Id);
    }
}
