using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetActas;

public record ActaResumenDto(Guid Id, string NumeroResolucionInterna, DateTime FechaEjecucion, string EmpresaResiduosBiocontaminados);

public record GetActasQuery(DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<ActaResumenDto>>>;

public class GetActasQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetActasQuery, Result<PaginatedList<ActaResumenDto>>>
{
    public async Task<Result<PaginatedList<ActaResumenDto>>> Handle(GetActasQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.DIGEMIDActasDestruccion.AsNoTracking().AsQueryable();


        if (request.FechaInicio.HasValue)
        {
            query = query.Where(a => a.FechaEjecucion >= request.FechaInicio.Value);
        }
        if (request.FechaFin.HasValue)
        {
            query = query.Where(a => a.FechaEjecucion <= request.FechaFin.Value);
        }

        var projectedQuery = query
            .OrderByDescending(a => a.FechaEjecucion)
            .Select(a => new ActaResumenDto(a.Id, a.NumeroResolucionInterna, a.FechaEjecucion, a.EmpresaResiduosBiocontaminados));

        var paginatedList = await PaginatedList<ActaResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
