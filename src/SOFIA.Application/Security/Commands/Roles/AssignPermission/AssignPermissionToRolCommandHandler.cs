using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.AssignPermission;

public class AssignPermissionToRolCommandHandler(IApplicationDbContext context) : IRequestHandler<AssignPermissionToRolCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AssignPermissionToRolCommand request, CancellationToken cancellationToken)
    {
        var rolExists = await context.Roles.AnyAsync(r => r.Id == request.RolId && !r.IsDeleted, cancellationToken);
        if (!rolExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Rol.NotFound", "El rol especificado no existe."));
        }

        // Check including soft-deleted rows to avoid unique constraint violations on restore
        var existing = await context.PermisosRol
            .IgnoreQueryFilters()
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
            return Result.Success(existing.Id);
        }

        var result = PermisoRol.Create(request.RolId, request.ModuloSistema, request.Accion);
        if (result.IsFailure)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.PermisosRol.Add(result.Value!);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value!.Id);
    }
}
