using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.RemoveSucursal;

public class RemoveSucursalFromCuentaCommandHandler(IApplicationDbContext context) : IRequestHandler<RemoveSucursalFromCuentaCommand, Result>
{
    public async Task<Result> Handle(RemoveSucursalFromCuentaCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .Include(c => c.Sucursales)
            .FirstOrDefaultAsync(c => c.Id == request.CuentaId && !c.IsDeleted, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure(Error.NotFound("Auth.CuentaNotFound", "La cuenta no existe."));
        }

        cuenta.RemoveSucursal(request.SucursalId);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
