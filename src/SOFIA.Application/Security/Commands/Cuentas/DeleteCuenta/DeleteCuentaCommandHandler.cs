using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.DeleteCuenta;

public class DeleteCuentaCommandHandler(IApplicationDbContext context)
    : IRequestHandler<DeleteCuentaCommand, Result>
{
    public async Task<Result> Handle(DeleteCuentaCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure(Error.NotFound("Cuenta.NotFound", "La cuenta especificada no existe."));
        }

        _ = context.Cuentas.Remove(cuenta);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
