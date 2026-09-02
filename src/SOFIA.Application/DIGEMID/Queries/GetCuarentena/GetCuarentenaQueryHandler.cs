using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetCuarentena;

public class GetCuarentenaQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetCuarentenaQuery, Result<PaginatedList<CuarentenaResumenDto>>>
{
    public async Task<Result<PaginatedList<CuarentenaResumenDto>>> Handle(GetCuarentenaQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.DigemidInventarioCuarentena.AsNoTracking().AsQueryable();

        if (request.SucursalId.HasValue)
        {
            query = query.Where(c => c.SucursalId == request.SucursalId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.EstadoResolucion))
        {
            query = query.Where(c => c.EstadoResolucion == request.EstadoResolucion);
        }

        query = query.WhereDateRange(c => c.FechaIngresoCuarentena, request.FechaInicio, request.FechaFin);

        var projectedQuery = query
            .OrderByDescending(c => c.FechaIngresoCuarentena)
            .Select(c => new CuarentenaResumenDto(c.Id, c.SucursalId, c.LoteId, c.CantidadAislada, c.MotivoAislamiento, c.EstadoResolucion, c.FechaIngresoCuarentena));

        var paginatedList = await PaginatedList<CuarentenaResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
