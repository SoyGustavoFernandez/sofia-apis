using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Servicios.Queries.GetServicios;

public record ServicioResumenDto(Guid Id, Guid ClienteId, Guid ProductoId, DateTime FechaHoraProgramada, string EstadoCita);

public record GetServiciosQuery(string? EstadoCita, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<ServicioResumenDto>>>;

public class GetServiciosQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetServiciosQuery, Result<PaginatedList<ServicioResumenDto>>>
{
    public async Task<Result<PaginatedList<ServicioResumenDto>>> Handle(GetServiciosQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.ServiciosAgenda.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(request.EstadoCita))
        {
            query = query.Where(s => s.EstadoCita == request.EstadoCita);
        }

        if (request.FechaInicio.HasValue)
        {
            query = query.Where(s => s.FechaHoraProgramada >= request.FechaInicio.Value);
        }
        if (request.FechaFin.HasValue)
        {
            query = query.Where(s => s.FechaHoraProgramada <= request.FechaFin.Value);
        }

        var projectedQuery = query
            .OrderByDescending(s => s.FechaHoraProgramada)
            .Select(s => new ServicioResumenDto(s.Id, s.ClienteId, s.ProductoId, s.FechaHoraProgramada, s.EstadoCita));

        var paginatedList = await PaginatedList<ServicioResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
