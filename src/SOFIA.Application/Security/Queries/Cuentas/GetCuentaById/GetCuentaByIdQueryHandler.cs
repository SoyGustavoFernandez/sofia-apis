using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Cuentas.GetCuentaById;

public class GetCuentaByIdQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetCuentaByIdQuery, Result<CuentaDto>>
{
    public async Task<Result<CuentaDto>> Handle(GetCuentaByIdQuery request, CancellationToken cancellationToken)
    {
        var cuenta = await context.Cuentas
            .Include(c => c.Empleado)
            .Include(c => c.Roles)
            .Include(c => c.Sucursales)
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == request.Id, cancellationToken);

        if (cuenta is null)
            return Result.Failure<CuentaDto>(Error.NotFound("Cuenta.NotFound", "La cuenta especificada no existe."));

        return Result.Success(new CuentaDto(
            cuenta.Id,
            cuenta.EmpleadoId,
            cuenta.Empleado!.Nombres + " " + cuenta.Empleado.Apellido_Paterno + " " + cuenta.Empleado.Apellido_Materno,
            cuenta.NombreUsuario,
            cuenta.CuentaActiva,
            cuenta.RequiereCambioClave,
            cuenta.IntentosFallidos,
            cuenta.BloqueadoHasta,
            cuenta.CreatedAt,
            cuenta.Roles.Select(r => new RolResponse(r.Id, r.NombreRol, r.Descripcion, r.NivelJerarquia, r.CreatedAt)).ToList(),
            cuenta.Sucursales.Select(s => new SucursalAsignadaDto(s.Id, s.Nombre)).ToList()));
    }
}
