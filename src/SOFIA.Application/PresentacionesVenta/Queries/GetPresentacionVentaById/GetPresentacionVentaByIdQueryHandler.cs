using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.PresentacionesVenta.Queries.GetPresentacionVentaById;

public class GetPresentacionVentaByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPresentacionVentaByIdQuery, Result<PresentacionVentaDto>>
{
    public async Task<Result<PresentacionVentaDto>> Handle(GetPresentacionVentaByIdQuery request, CancellationToken cancellationToken)
    {
        var entity = await context.PresentacionesVenta
            .AsNoTracking()
            .Include(x => x.Producto)
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (entity == null)
        {
            return Result.Failure<PresentacionVentaDto>(Error.NotFound("PresentacionVenta.NotFound", $"Presentacion de Venta with ID {request.Id} was not found."), 404);
        }

        var dto = new PresentacionVentaDto(
            entity.Id,
            entity.ProductoId,
            entity.Producto?.NombreComercial ?? "Unknown",
            entity.UnidadVentaId,
            entity.Descripcion,
            entity.CantidadUnidadesBase,
            entity.PrecioVenta);

        return Result.Success(dto);
    }
}
