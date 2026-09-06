using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.AssignSucursal;

public class AssignSucursalToCuentaCommandHandler(IApplicationDbContext context) : IRequestHandler<AssignSucursalToCuentaCommand, Result>
{
    public async Task<Result> Handle(AssignSucursalToCuentaCommand request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .Include(c => c.Sucursales)
            .FirstOrDefaultAsync(c => c.Id == request.CuentaId && !c.IsDeleted, cancellationToken);

        if (cuenta is null)
        {
            return Result.Failure(Error.NotFound("Auth.CuentaNotFound", "La cuenta no existe."));
        }

        var sucursal = await context.Sucursales
            .FirstOrDefaultAsync(s => s.Id == request.SucursalId && !s.IsDeleted, cancellationToken);

        if (sucursal is null)
        {
            return Result.Failure(Error.NotFound("Sucursal.NotFound", "La sucursal no existe."));
        }

        cuenta.AddSucursal(sucursal);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
