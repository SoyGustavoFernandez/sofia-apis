using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.AssignRol;

public class AssignRolToUserCommandHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<AssignRolToUserCommand, Result>
{
    public async Task<Result> Handle(AssignRolToUserCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .Include(c => c.Roles)
            .FirstOrDefaultAsync(c => c.Id == request.CuentaId && !c.IsDeleted, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure(Error.NotFound("Auth.CuentaNotFound", "La cuenta no existe."));
        }

        // Nobody can change their own roles, otherwise any role manager could promote themselves
        if (cuenta.EmpleadoId.ToString() == currentUser.Id)
        {
            return Result.Failure(Error.Forbidden("Rol.AutoAsignacion", "No puedes modificar tus propios roles."), 403);
        }

        var rol = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RolId && !r.IsDeleted, cancellationToken);

        if (rol is null)
        {
            return Result.Failure(Error.NotFound("Rol.NotFound", "El rol no existe."));
        }

        if (rol.EsAdmin && !currentUser.IsInRole(Rol.AdminRoleName))
        {
            return Result.Failure(Error.Forbidden("Rol.AdminReservado", "Solo un administrador puede asignar o quitar el rol Admin."), 403);
        }

        cuenta.AddRol(rol);

        // Rejects the account's current access token so the new role set applies on the next refresh
        cuenta.InvalidateSecurityStamp();

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
