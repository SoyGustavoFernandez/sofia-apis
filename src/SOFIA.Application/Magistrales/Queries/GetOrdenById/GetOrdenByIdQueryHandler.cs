using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Application.Magistrales.Queries.GetOrdenes;

namespace SOFIA.Application.Magistrales.Queries.GetOrdenById;

public class GetOrdenByIdQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess) : IRequestHandler<GetOrdenByIdQuery, Result<OrdenResumenDto>>
{
    public async Task<Result<OrdenResumenDto>> Handle(GetOrdenByIdQuery request, CancellationToken cancellationToken)
    {
        var allowed = await sucursalAccess.GetAllowedSucursalesAsync(cancellationToken);
        var orden = await context.MagistralesOrdenesProduccion
            .AsNoTracking()
            .WhereSucursalIn(o => o.SucursalId, allowed)
            .FirstOrDefaultAsync(o => o.Id == request.Id && !o.IsDeleted, cancellationToken);

        if (orden == null)
        {
            return Result.Failure<OrdenResumenDto>(Error.NotFound("OrdenMagistral.NotFound", "Orden Magistral not found."), 404);
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
