using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Queries.GetSesiones;

public class GetSesionesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetSesionesQuery, Result<PaginatedList<SesionResumenDto>>>
{
    public async Task<Result<PaginatedList<SesionResumenDto>>> Handle(GetSesionesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.POSSesionesCaja.AsNoTracking().AsQueryable();

        if (request.SucursalId.HasValue && request.SucursalId.Value != Guid.Empty)
        {
            query = query.Where(s => s.SucursalId == request.SucursalId.Value);
        }
        if (request.EstadoSesion.HasValue)
        {
            query = query.Where(s => s.EstadoSesion == request.EstadoSesion);
        }

        if (request.FechaInicio.HasValue)
        {
            query = query.Where(s => s.FechaHoraApertura >= request.FechaInicio.Value);
        }
        if (request.FechaFin.HasValue)
        {
            query = query.Where(s => s.FechaHoraApertura <= request.FechaFin.Value);
        }

        var projectedQuery = query
            .OrderByDescending(s => s.FechaHoraApertura)
            .Select(s => new SesionResumenDto(s.Id, s.SucursalId, s.EmpleadoId, s.FechaHoraApertura, s.FechaHoraCierre, s.MontoAperturaEfectivo, s.MontoCierreCalculado, s.EstadoSesion));

        var paginatedList = await PaginatedList<SesionResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
