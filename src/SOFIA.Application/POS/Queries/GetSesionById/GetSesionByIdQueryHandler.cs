using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Application.POS.Queries.GetSesiones;

namespace SOFIA.Application.POS.Queries.GetSesionById;

public class GetSesionByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetSesionByIdQuery, Result<SesionResumenDto>>
{
    public async Task<Result<SesionResumenDto>> Handle(GetSesionByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await (
            from s in context.POSSesionesCaja.AsNoTracking().Where(o => o.Id == request.Id && !o.IsDeleted)
            join suc in context.Sucursales on s.SucursalId equals suc.Id into sucGroup
            from suc in sucGroup.DefaultIfEmpty()
            join emp in context.Empleados on s.EmpleadoId equals emp.Id into empGroup
            from emp in empGroup.DefaultIfEmpty()
            select new SesionResumenDto(
                s.Id,
                s.SucursalId,
                suc != null ? suc.Nombre : string.Empty,
                s.EmpleadoId,
                emp != null ? emp.Nombres + " " + emp.Apellido_Paterno : string.Empty,
                s.FechaHoraApertura,
                s.FechaHoraCierre,
                s.MontoAperturaEfectivo,
                s.MontoCierreCalculado,
                s.MontoCierreDeclarado,
                s.DiferenciaArqueo,
                s.EstadoSesion))
            .FirstOrDefaultAsync(cancellationToken);

        if (dto == null)
        {
            return Result.Failure<SesionResumenDto>(Error.NotFound("SesionCaja.NotFound", "Sesion de Caja not found."));
        }

        return Result.Success(dto);
    }
}
