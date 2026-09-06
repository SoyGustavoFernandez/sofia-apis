using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.UpdateCuenta;

public class UpdateCuentaCommandHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateCuentaCommand, Result>
{
    public async Task<Result> Handle(UpdateCuentaCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure(Error.NotFound("Cuenta.NotFound", "La cuenta especificada no existe."));
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
