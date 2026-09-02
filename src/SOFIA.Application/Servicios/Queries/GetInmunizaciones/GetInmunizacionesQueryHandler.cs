using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Queries.GetInmunizaciones;

public class GetInmunizacionesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetInmunizacionesQuery, Result<PaginatedList<InmunizacionResumenDto>>>
{
    public async Task<Result<PaginatedList<InmunizacionResumenDto>>> Handle(GetInmunizacionesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ServiciosClinicosInmunizacion.AsNoTracking().AsQueryable();

        if (request.ClienteId.HasValue)
        {
            query = query.Where(i => i.ClienteId == request.ClienteId.Value);
        }

        query = query.WhereDateRange(i => i.FechaAdmnFisica, request.FechaInicio, request.FechaFin);

        var projectedQuery = query
            .OrderByDescending(i => i.FechaAdmnFisica)
            .Select(i => new InmunizacionResumenDto(i.Id, i.ClienteId, i.FechaAdmnFisica, i.ViaAdministracion));

        var paginatedList = await PaginatedList<InmunizacionResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
