using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Commands.DeleteSucursal;

public class DeleteSucursalCommandHandler(IApplicationDbContext context) : IRequestHandler<DeleteSucursalCommand, Result>
{
    public async Task<Result> Handle(DeleteSucursalCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Sucursales
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Sucursal.NotFound", $"Sucursal with ID {request.Id} was not found."), 404);
        }

        if (await IsReferencedAsync(request.Id, cancellationToken))
        {
            return Result.Failure(
                Error.Conflict("Sucursal.InUse", "Cannot delete a branch referenced by employees, accounts, roles, stock, cash sessions, fiscal series, sales, transfers, quarantine records or compounding orders."),
                409);
        }

        _ = context.Sucursales.Remove(entity);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(204);
    }

    private async Task<bool> IsReferencedAsync(Guid id, CancellationToken cancellationToken) =>
        await context.Empleados.AnyAsync(e => e.Sucursal_Base_ID == id && !e.IsDeleted, cancellationToken)
        || await context.Cuentas.AnyAsync(c => !c.IsDeleted && c.Sucursales.Any(s => s.Id == id), cancellationToken)
        || await context.Roles.AnyAsync(r => !r.IsDeleted && r.Sucursales.Any(s => s.Id == id), cancellationToken)
        || await context.LotesEnSucursal.AnyAsync(i => i.SucursalId == id && !i.IsDeleted, cancellationToken)
        || await context.POSSesionesCaja.AnyAsync(p => p.SucursalId == id && !p.IsDeleted, cancellationToken)
        || await context.SUNATSeriesFiscales.AnyAsync(s => s.SucursalId == id && !s.IsDeleted, cancellationToken)
        || await context.Ventas.AnyAsync(v => v.SucursalId == id && !v.IsDeleted, cancellationToken)
        || await context.Transferencias.AnyAsync(t => (t.SucursalOrigenId == id || t.SucursalDestinoId == id) && !t.IsDeleted, cancellationToken)
        || await context.DigemidInventarioCuarentena.AnyAsync(d => d.SucursalId == id && !d.IsDeleted, cancellationToken)
        || await context.MagistralesOrdenesProduccion.AnyAsync(m => m.SucursalId == id && !m.IsDeleted, cancellationToken);
}
