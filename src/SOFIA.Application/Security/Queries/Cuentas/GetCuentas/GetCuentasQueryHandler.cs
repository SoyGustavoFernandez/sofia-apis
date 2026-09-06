using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Cuentas.GetCuentas;

public class GetCuentasQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetCuentasQuery, Result<PaginatedList<CuentaDto>>>
{
    public async Task<Result<PaginatedList<CuentaDto>>> Handle(GetCuentasQuery request, CancellationToken cancellationToken)
    {
        var query = context.Cuentas
            .Include(c => c.Empleado)
            .Include(c => c.Roles)
            .Include(c => c.Sucursales)
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.NombreUsuario))
        {
            query = query.Where(c => c.NombreUsuario.Contains(request.NombreUsuario));
        }

        if (!string.IsNullOrWhiteSpace(request.NombreEmpleado))
        {
            query = query.Where(c =>
                (c.Empleado!.Nombres + " " + c.Empleado.Apellido_Paterno + " " + c.Empleado.Apellido_Materno)
                    .Contains(request.NombreEmpleado));
        }

        if (request.CuentaActiva.HasValue)
        {
            query = query.Where(c => c.CuentaActiva == request.CuentaActiva.Value);
        }

        if (request.Bloqueado.HasValue)
        {
            query = query.Where(c => request.Bloqueado.Value
                ? c.BloqueadoHasta != null && c.BloqueadoHasta > DateTimeOffset.UtcNow
                : c.BloqueadoHasta == null || c.BloqueadoHasta <= DateTimeOffset.UtcNow);
        }

        var paginated = await PaginatedList<CuentaDto>.CreateAsync(
            query.OrderBy(c => c.NombreUsuario).Select(c => new CuentaDto(
                c.Id,
                c.EmpleadoId,
                c.Empleado!.Nombres + " " + c.Empleado.Apellido_Paterno + " " + c.Empleado.Apellido_Materno,
                c.NombreUsuario,
                c.CuentaActiva,
                c.RequiereCambioClave,
                c.IntentosFallidos,
                c.BloqueadoHasta,
                c.CreatedAt,
                c.Roles.Select(r => new RolResponse(r.Id, r.NombreRol, r.Descripcion, r.NivelJerarquia, r.CreatedAt)).ToList(),
                c.Sucursales.Select(s => new SucursalAsignadaDto(s.Id, s.Nombre)).ToList())),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginated);
    }
}
