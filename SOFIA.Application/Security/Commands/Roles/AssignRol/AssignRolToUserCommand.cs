using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.AssignRol;

public record AssignRolToUserCommand(Guid CuentaId, Guid RolId) : IRequest<Result>;

public class AssignRolToUserCommandHandler(IApplicationDbContext context) : IRequestHandler<AssignRolToUserCommand, Result>
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

        var rol = await context.Roles
            .FirstOrDefaultAsync(r => r.Id == request.RolId && !r.IsDeleted, cancellationToken);

        if (rol is null)
        {
            return Result.Failure(Error.NotFound("Rol.NotFound", "El rol no existe."));
        }

        cuenta.AddRol(rol);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
