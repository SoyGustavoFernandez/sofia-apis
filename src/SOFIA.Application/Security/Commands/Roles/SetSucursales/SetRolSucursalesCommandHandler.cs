using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.SetSucursales;

public class SetRolSucursalesCommandHandler(IApplicationDbContext context)
    : IRequestHandler<SetRolSucursalesCommand, Result>
{
    public async Task<Result> Handle(SetRolSucursalesCommand request, CancellationToken cancellationToken)
    {
        var rolExists = await context.Roles
            .AnyAsync(r => r.Id == request.RolId && !r.IsDeleted, cancellationToken);

        if (!rolExists)
        {
            return Result.Failure(Error.NotFound("Rol.NotFound", "El rol no existe."), 404);
        }

        // RolSucursal has no tenant filter, so every branch must be checked against the tenant-filtered set
        var sucursalIds = request.SucursalIds.Distinct().ToList();
        var foundCount = await context.Sucursales.CountAsync(s => sucursalIds.Contains(s.Id), cancellationToken);
        if (foundCount != sucursalIds.Count)
        {
            return Result.Failure(Error.NotFound("Sucursal.NotFound", "La sucursal no existe."), 404);
        }

        var existing = await context.RolesSucursales
            .Where(rs => rs.RolId == request.RolId)
            .ToListAsync(cancellationToken);

        context.RolesSucursales.RemoveRange(existing);

        var newAssignments = sucursalIds.Select(sucursalId => RolSucursal.Create(request.RolId, sucursalId));

        await context.RolesSucursales.AddRangeAsync(newAssignments, cancellationToken);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
