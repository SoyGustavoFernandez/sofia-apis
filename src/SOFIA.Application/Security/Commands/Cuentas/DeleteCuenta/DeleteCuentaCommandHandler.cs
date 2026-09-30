using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Cuentas.DeleteCuenta;

public class DeleteCuentaCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<DeleteCuentaCommand, Result>
{
    public async Task<Result> Handle(DeleteCuentaCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .Include(c => c.Roles)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure(Error.NotFound("Cuenta.NotFound", "La cuenta especificada no existe."), 404);
        }

        if (cuenta.EsAdmin && !currentUser.IsInRole(Rol.AdminRoleName))
        {
            return Result.Failure(Error.Forbidden("Cuenta.AdminProtegida", "Solo un administrador puede modificar o eliminar la cuenta de un administrador."), 403);
        }

        _ = context.Cuentas.Remove(cuenta);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
