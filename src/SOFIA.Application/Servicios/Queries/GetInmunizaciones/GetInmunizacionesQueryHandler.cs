using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Queries.GetInmunizaciones;

public class GetInmunizacionesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetInmunizacionesQuery, Result<PaginatedList<InmunizacionResumenDto>>>
{
    public async Task<Result<PaginatedList<InmunizacionResumenDto>>> Handle(GetInmunizacionesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ServiciosClinicosInmunizacion.AsNoTracking().AsQueryable();

        if (request.ClienteId.HasValue && request.ClienteId.Value != Guid.Empty)
        {
            query = query.Where(i => i.ClienteId == request.ClienteId.Value);
        }

        if (request.FechaInicio.HasValue)
        {
            query = query.Where(i => i.FechaAdmnFisica >= request.FechaInicio.Value);
        }
        if (request.FechaFin.HasValue)
        {
            query = query.Where(i => i.FechaAdmnFisica <= request.FechaFin.Value);
        }

        var projectedQuery = query
            .OrderByDescending(i => i.FechaAdmnFisica)
            .Select(i => new InmunizacionResumenDto(i.Id, i.ClienteId, i.FechaAdmnFisica, i.ViaAdministracion));

        var paginatedList = await PaginatedList<InmunizacionResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
