using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.RemoveRol;

public class RemoveRolFromUserCommandHandler(IApplicationDbContext context) : IRequestHandler<RemoveRolFromUserCommand, Result>
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

        cuenta.RemoveRol(request.RolId);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
