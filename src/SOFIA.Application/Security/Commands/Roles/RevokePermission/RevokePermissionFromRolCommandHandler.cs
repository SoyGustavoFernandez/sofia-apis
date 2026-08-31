using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.RevokePermission;

public class RevokePermissionFromRolCommandHandler(IApplicationDbContext context) : IRequestHandler<RevokePermissionFromRolCommand, Result>
{
    public async Task<Result> Handle(RevokePermissionFromRolCommand request, CancellationToken cancellationToken)
    {
        var permiso = await context.PermisosRol
            .FirstOrDefaultAsync(p => p.Id == request.PermisoId && !p.IsDeleted, cancellationToken);

        if (permiso is null)
        {
            return Result.Failure(Error.NotFound("Permiso.NotFound", "El permiso especificado no existe."));
        }

        permiso.IsDeleted = true;
        permiso.DeletedAt = DateTimeOffset.UtcNow;

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
