using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetCuarentena;

public record CuarentenaResumenDto(Guid Id, Guid SucursalId, Guid LoteId, decimal CantidadAislada, string MotivoAislamiento, string EstadoResolucion, DateTime FechaIngresoCuarentena);

public record GetCuarentenaQuery(Guid? SucursalId, string? EstadoResolucion, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<CuarentenaResumenDto>>>;

public class GetCuarentenaQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetCuarentenaQuery, Result<PaginatedList<CuarentenaResumenDto>>>
{
    public async Task<Result<PaginatedList<CuarentenaResumenDto>>> Handle(GetCuarentenaQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.DIGEMIDInventarioCuarentena.AsNoTracking().AsQueryable();

        if (request.SucursalId.HasValue && request.SucursalId.Value != Guid.Empty)
        {
            query = query.Where(c => c.SucursalId == request.SucursalId.Value);
        }
        if (!string.IsNullOrWhiteSpace(request.EstadoResolucion))
        {
            query = query.Where(c => c.EstadoResolucion == request.EstadoResolucion);
        }

        if (request.FechaInicio.HasValue)
        {
            query = query.Where(c => c.FechaIngresoCuarentena >= request.FechaInicio.Value);
        }
        if (request.FechaFin.HasValue)
        {
            query = query.Where(c => c.FechaIngresoCuarentena <= request.FechaFin.Value);
        }

        var projectedQuery = query
            .OrderByDescending(c => c.FechaIngresoCuarentena)
            .Select(c => new CuarentenaResumenDto(c.Id, c.SucursalId, c.LoteId, c.CantidadAislada, c.MotivoAislamiento, c.EstadoResolucion, c.FechaIngresoCuarentena));

        var paginatedList = await PaginatedList<CuarentenaResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
