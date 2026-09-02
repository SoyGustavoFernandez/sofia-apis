using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

public class GetDevolucionesQueryHandler(IApplicationDbContext dbContext) : IRequestHandler<GetDevolucionesQuery, Result<PaginatedList<DevolucionResumenDto>>>
{
    public async Task<Result<PaginatedList<DevolucionResumenDto>>> Handle(GetDevolucionesQuery request, CancellationToken cancellationToken)
    {
        var query = dbContext.Devoluciones.AsNoTracking().AsQueryable();

        if (request.EmpleadoAutorizaId.HasValue)
        {
            query = query.Where(d => d.EmpleadoAutorizaId == request.EmpleadoAutorizaId.Value);
        }

        query = query.WhereDateRange(d => d.FechaDevolucion, request.FechaInicio, request.FechaFin);

        var projectedQuery = query
            .OrderByDescending(d => d.FechaDevolucion)
            .Select(d => new DevolucionResumenDto(d.Id, d.ComprobanteOrigenId, d.EmpleadoAutorizaId, d.FechaDevolucion, d.MotivoSunatCatalogo, d.SustentoDescriptivo));

        var paginatedList = await PaginatedList<DevolucionResumenDto>.CreateAsync(projectedQuery, request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
