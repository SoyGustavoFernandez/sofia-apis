using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Queries.GetServicios;

public class GetServiciosQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetServiciosQuery, Result<PaginatedList<ServicioResumenDto>>>
{
    public async Task<Result<PaginatedList<ServicioResumenDto>>> Handle(GetServiciosQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ServiciosAgenda.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EstadoCita))
        {
            query = query.Where(s => s.EstadoCita == request.EstadoCita);
        }

        query = query.WhereDateRange(s => s.FechaHoraProgramada, request.FechaInicio, request.FechaFin);

        var projectedQuery = query
            .OrderByDescending(s => s.FechaHoraProgramada)
            .Select(s => new ServicioResumenDto(s.Id, s.ClienteId, s.ProductoId, s.FechaHoraProgramada, s.EstadoCita));

        var paginatedList = await PaginatedList<ServicioResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
