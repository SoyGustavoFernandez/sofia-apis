using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.RemoveRol;

public class RemoveRolFromUserCommandHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<RemoveRolFromUserCommand, Result>
{
    public async Task<Result> Handle(RemoveRolFromUserCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .Include(c => c.Roles)
            .FirstOrDefaultAsync(c => c.Id == request.CuentaId && !c.IsDeleted, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure(Error.NotFound("Auth.CuentaNotFound", "La cuenta no existe."));
        }

        if (cuenta.EmpleadoId.ToString() == currentUser.Id)
        {
            return Result.Failure(Error.Forbidden("Rol.AutoAsignacion", "No puedes modificar tus propios roles."), 403);
        }

        var rol = cuenta.Roles.FirstOrDefault(r => r.Id == request.RolId);
        if (rol is null)
        {
            return Result.Success();
        }

        // Only an Admin can demote another Admin; with self-removal blocked, the acting Admin always remains
        if (rol.EsAdmin && !currentUser.IsInRole(Rol.AdminRoleName))
        {
            return Result.Failure(Error.Forbidden("Rol.AdminReservado", "Solo un administrador puede asignar o quitar el rol Admin."), 403);
        }

        cuenta.RemoveRol(request.RolId);

        // A removed role must stop working now, not when the access token expires
        cuenta.InvalidateSecurityStamp();

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
