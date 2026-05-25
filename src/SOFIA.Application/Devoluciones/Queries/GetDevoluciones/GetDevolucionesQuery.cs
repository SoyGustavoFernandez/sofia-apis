using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

public record DevolucionResumenDto(
    Guid Id,
    Guid ComprobanteOrigenId,
    Guid EmpleadoAutorizaId,
    DateTime FechaDevolucion,
    string MotivoSunatCatalogo,
    string SustentoDescriptivo
);

public record GetDevolucionesQuery(Guid? EmpleadoAutorizaId, DateTime? FechaInicio, DateTime? FechaFin, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<DevolucionResumenDto>>>;

public class GetDevolucionesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetDevolucionesQuery, Result<PaginatedList<DevolucionResumenDto>>>
{
    public async Task<Result<PaginatedList<DevolucionResumenDto>>> Handle(GetDevolucionesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Devoluciones.AsNoTracking().AsQueryable();

        if (request.EmpleadoAutorizaId.HasValue && request.EmpleadoAutorizaId.Value != Guid.Empty)
        {
            query = query.Where(d => d.EmpleadoAutorizaId == request.EmpleadoAutorizaId.Value);
        }

        if (request.FechaInicio.HasValue)
        {
            query = query.Where(d => d.FechaDevolucion >= request.FechaInicio.Value);
        }
        if (request.FechaFin.HasValue)
        {
            query = query.Where(d => d.FechaDevolucion <= request.FechaFin.Value);
        }

        var projectedQuery = query
            .OrderByDescending(d => d.FechaDevolucion)
            .Select(d => new DevolucionResumenDto(d.Id, d.ComprobanteOrigenId, d.EmpleadoAutorizaId, d.FechaDevolucion, d.MotivoSunatCatalogo, d.SustentoDescriptivo));

        var paginatedList = await PaginatedList<DevolucionResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
