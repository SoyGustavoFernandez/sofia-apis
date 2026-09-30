using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.DeleteRol;

public class DeleteRolCommandHandler(IApplicationDbContext context, ICurrentUser currentUser, IPermissionCache permissionCache) : IRequestHandler<DeleteRolCommand, Result>
{
    public async Task<Result> Handle(DeleteRolCommand request, CancellationToken cancellationToken)
    {
        var rol = await context.Roles
            .Include(r => r.Cuentas)
            .FirstOrDefaultAsync(r => r.Id == request.Id && !r.IsDeleted, cancellationToken);

        if (rol is null)
        {
            return Result.Failure(Error.NotFound("Rol.NotFound", "El rol especificado no existe."), 404);
        }

        if (rol.Cuentas.Any(c => !c.IsDeleted))
        {
            return Result.Failure(Error.Conflict("Rol.InUse", "No se puede eliminar un rol que tiene usuarios activos vinculados."));
        }

        _ = context.Roles.Remove(rol);

        _ = await context.SaveChangesAsync(cancellationToken);

        // A role recreated later with the same name must not inherit the deleted role's cached permissions
        permissionCache.InvalidateForCurrentEmpresa(currentUser, rol.NombreRol);

        return Result.Success();
    }
}
