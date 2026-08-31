using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Queries.GetRecetas;

public class GetRecetasQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetRecetasQuery, Result<PaginatedList<RecetaResumenDto>>>
{
    public async Task<Result<PaginatedList<RecetaResumenDto>>> Handle(GetRecetasQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Recetas.AsNoTracking().AsQueryable();

        if (request.ClienteId.HasValue && request.ClienteId.Value != Guid.Empty)
        {
            query = query.Where(r => r.ClienteId == request.ClienteId.Value);
        }

        if (request.FechaInicio.HasValue)
        {
            query = query.Where(r => r.FechaExpedicion >= DateOnly.FromDateTime(request.FechaInicio.Value));
        }
        if (request.FechaFin.HasValue)
        {
            query = query.Where(r => r.FechaExpedicion <= DateOnly.FromDateTime(request.FechaFin.Value));
        }

        var projectedQuery = query
            .OrderByDescending(r => r.FechaExpedicion)
            .Select(r => new RecetaResumenDto(r.Id, r.ClienteId, r.MedicoId, r.FechaExpedicion, r.IndicacionesUso));

        var paginatedList = await PaginatedList<RecetaResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
