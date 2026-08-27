using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.AssignPermission;

public record AssignPermissionToRolCommand(Guid RolId, string ModuloSistema, string Accion) : ICommand<Guid>;

public class AssignPermissionToRolCommandHandler(IApplicationDbContext context) : IRequestHandler<AssignPermissionToRolCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(AssignPermissionToRolCommand request, CancellationToken cancellationToken)
    {
        var rolExists = await context.Roles.AnyAsync(r => r.Id == request.RolId && !r.IsDeleted, cancellationToken);
        if (!rolExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Rol.NotFound", "El rol especificado no existe."));
        }

        // Check whether the permission is already assigned to the role
        var alreadyExists = await context.PermisosRol
            .AnyAsync(p => p.RolId == request.RolId &&
                           p.ModuloSistema == request.ModuloSistema &&
                           p.Accion == request.Accion &&
                           !p.IsDeleted, cancellationToken);

        if (alreadyExists)
        {
            return Result.Failure<Guid>(Error.Conflict("Permiso.Duplicate", "Este permiso ya está asignado a este rol."));
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
