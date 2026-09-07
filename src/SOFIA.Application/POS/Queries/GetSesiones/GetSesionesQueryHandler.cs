using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Queries.GetSesiones;

public class GetSesionesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetSesionesQuery, Result<PaginatedList<SesionResumenDto>>>
{
    public async Task<Result<PaginatedList<SesionResumenDto>>> Handle(GetSesionesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.POSSesionesCaja.AsNoTracking().AsQueryable();

        if (request.SucursalId.HasValue)
        {
            query = query.Where(s => s.SucursalId == request.SucursalId.Value);
        }
        if (request.EstadoSesion.HasValue)
        {
            query = query.Where(s => s.EstadoSesion == request.EstadoSesion);
        }

        query = query.WhereDateRange(s => s.FechaHoraApertura, request.FechaInicio, request.FechaFin);

        var projectedQuery =
            from s in query.OrderByDescending(s => s.FechaHoraApertura)
            join suc in dbContext.Sucursales on s.SucursalId equals suc.Id into sucGroup
            from suc in sucGroup.DefaultIfEmpty()
            join emp in dbContext.Empleados on s.EmpleadoId equals emp.Id into empGroup
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
                s.EstadoSesion);

        var paginatedList = await PaginatedList<SesionResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
