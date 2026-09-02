using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetActas;

public class GetActasQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetActasQuery, Result<PaginatedList<ActaResumenDto>>>
{
    public async Task<Result<PaginatedList<ActaResumenDto>>> Handle(GetActasQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.DIGEMIDActasDestruccion.AsNoTracking().AsQueryable();

        query = query.WhereDateRange(a => a.FechaEjecucion, request.FechaInicio, request.FechaFin);

        var projectedQuery = query
            .OrderByDescending(a => a.FechaEjecucion)
            .Select(a => new ActaResumenDto(a.Id, a.NumeroResolucionInterna, a.FechaEjecucion, a.EmpresaResiduosBiocontaminados));

        var paginatedList = await PaginatedList<ActaResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
