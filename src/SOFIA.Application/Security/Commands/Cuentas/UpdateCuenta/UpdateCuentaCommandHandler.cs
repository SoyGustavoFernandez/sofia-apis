using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Cuentas.UpdateCuenta;

public class UpdateCuentaCommandHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<UpdateCuentaCommand, Result>
{
    public async Task<Result> Handle(UpdateCuentaCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .Include(c => c.Roles)
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure(Error.NotFound("Cuenta.NotFound", "La cuenta especificada no existe."));
        }

        // Otherwise a non-admin could deactivate the owner's account and lock them out
        if (cuenta.EsAdmin && !currentUser.IsInRole(Rol.AdminRoleName))
        {
            return Result.Failure(Error.Forbidden("Cuenta.AdminProtegida", "Solo un administrador puede modificar o eliminar la cuenta de un administrador."), 403);
        }

        if (request.CuentaActiva.HasValue && cuenta.CuentaActiva != request.CuentaActiva.Value)
        {
            cuenta.ToggleActive();
        }

        if (request.ForzarCambioClave == true)
        {
            cuenta.ForcePasswordChange();
        }

        if (request.ResetearIntentos)
        {
            cuenta.ResetFailedAttempts();
        }

        _ = await context.SaveChangesAsync(cancellationToken);
        return Result.Success();
    }
}
