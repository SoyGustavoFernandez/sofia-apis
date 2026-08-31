using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Delivery.Queries.GetDespachos;

public class GetDespachosQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetDespachosQuery, Result<PaginatedList<DespachoResumenDto>>>
{
    public async Task<Result<PaginatedList<DespachoResumenDto>>> Handle(GetDespachosQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.DespachosDelivery.AsNoTracking().AsQueryable();

        if (request.EstadoDespacho.HasValue)
        {
            query = query.Where(d => d.EstadoDespacho == request.EstadoDespacho);
        }

        if (request.FechaInicio.HasValue)
        {
            query = query.Where(d => d.CreatedAt >= request.FechaInicio.Value);
        }
        if (request.FechaFin.HasValue)
        {
            query = query.Where(d => d.CreatedAt <= request.FechaFin.Value);
        }

        var projectedQuery = query
            .OrderByDescending(d => d.Id)
            .Select(d => new DespachoResumenDto(d.Id, d.VentaId, d.EstadoDespacho, d.DireccionEntrega));

        var paginatedList = await PaginatedList<DespachoResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
