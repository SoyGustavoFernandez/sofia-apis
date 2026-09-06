using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Roles.GetRolSucursales;

public class GetRolSucursalesQueryHandler(IApplicationDbContext context, ICurrentUser currentUser)
    : IRequestHandler<GetRolSucursalesQuery, Result<List<SucursalRolItem>>>
{
    public async Task<Result<List<SucursalRolItem>>> Handle(
        GetRolSucursalesQuery request,
        CancellationToken cancellationToken)
    {
        var rolExists = await context.Roles
            .AnyAsync(r => r.Id == request.RolId && !r.IsDeleted, cancellationToken);

        if (!rolExists)
        {
            return Result.Failure<List<SucursalRolItem>>(Error.NotFound("Rol.NotFound", "El rol no existe."));
        }

        var empresaId = Guid.TryParse(currentUser.EmpresaId, out var eid) ? eid : (Guid?)null;

        var assigned = await context.RolesSucursales
            .Where(rs => rs.RolId == request.RolId)
            .Select(rs => rs.SucursalId)
            .ToHashSetAsync(cancellationToken);

        var items = await context.Sucursales
            .Where(s => !s.IsDeleted && s.EmpresaId == empresaId)
            .OrderBy(s => s.Nombre)
            .Select(s => new SucursalRolItem(s.Id, s.Nombre, assigned.Contains(s.Id)))
            .ToListAsync(cancellationToken);

        return Result.Success(items);
    }
}
