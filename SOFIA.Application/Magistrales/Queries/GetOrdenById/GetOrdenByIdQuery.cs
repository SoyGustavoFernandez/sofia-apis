using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Application.Magistrales.Queries.GetOrdenes;

namespace SOFIA.Application.Magistrales.Queries.GetOrdenById;

public record GetOrdenByIdQuery(Guid Id) : IRequest<Result<OrdenResumenDto>>;

public class GetOrdenByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetOrdenByIdQuery, Result<OrdenResumenDto>>
{
    public async Task<Result<OrdenResumenDto>> Handle(GetOrdenByIdQuery request, CancellationToken cancellationToken)
    {
        var orden = await context.MagistralesOrdenesProduccion
            .AsNoTracking()
            .FirstOrDefaultAsync(o => o.Id == request.Id && !o.IsDeleted, cancellationToken);

        if (orden == null)
        {
            return Result.Failure<OrdenResumenDto>(Error.NotFound("OrdenMagistral.NotFound", "Orden Magistral not found."));
        }

        var dto = new OrdenResumenDto(
            orden.Id,
            orden.SucursalId,
            orden.ProductoResultanteId,
            orden.CantidadProducida,
            orden.EstadoProduccion.ToString(),
            orden.FechaPreparacion);

        return Result.Success(dto);
    }
}
