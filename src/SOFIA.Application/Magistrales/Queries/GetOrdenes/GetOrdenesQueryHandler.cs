using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Magistrales.Queries.GetOrdenes;

public class GetOrdenesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetOrdenesQuery, Result<PaginatedList<OrdenResumenDto>>>
{
    public async Task<Result<PaginatedList<OrdenResumenDto>>> Handle(GetOrdenesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.MagistralesOrdenesProduccion.AsNoTracking().AsQueryable();

        if (request.SucursalId.HasValue && request.SucursalId.Value != Guid.Empty)
        {
            query = query.Where(o => o.SucursalId == request.SucursalId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.EstadoProduccion))
        {
            query = query.Where(o => o.EstadoProduccion == request.EstadoProduccion);
        }

        query = query.WhereDateRange(o => o.FechaPreparacion, request.FechaInicio, request.FechaFin);

        var projectedQuery = query
            .OrderByDescending(o => o.FechaPreparacion)
            .Select(o => new OrdenResumenDto(o.Id, o.SucursalId, o.ProductoResultanteId, o.CantidadProducida, o.EstadoProduccion, o.FechaPreparacion));

        var paginatedList = await PaginatedList<OrdenResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
