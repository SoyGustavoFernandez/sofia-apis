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

        if (request.ClienteId.HasValue)
        {
            query = query.Where(r => r.ClienteId == request.ClienteId.Value);
        }

        if (request.MedicoId.HasValue)
        {
            query = query.Where(r => r.MedicoId == request.MedicoId.Value);
        }

        if (request.FechaInicio.HasValue)
        {
            query = query.Where(r => r.FechaExpedicion >= DateOnly.FromDateTime(request.FechaInicio.Value));
        }

        if (request.FechaFin.HasValue)
        {
            query = query.Where(r => r.FechaExpedicion <= DateOnly.FromDateTime(request.FechaFin.Value));
        }

        var projectedQuery =
            from r in query.OrderByDescending(r => r.FechaExpedicion)
            join p in dbContext.Pacientes on r.ClienteId equals p.Id into pGroup
            from p in pGroup.DefaultIfEmpty()
            join m in dbContext.ProfesionalesSalud on r.MedicoId equals m.Id into mGroup
            from m in mGroup.DefaultIfEmpty()
            select new RecetaResumenDto(
                r.Id,
                r.ClienteId,
                p != null ? p.NombreApellidos : string.Empty,
                r.MedicoId,
                m != null ? m.NombrePrescriptor : string.Empty,
                r.FechaExpedicion,
                r.RepeticionesMax,
                r.IndicacionesUso);

        var paginatedList = await PaginatedList<RecetaResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
