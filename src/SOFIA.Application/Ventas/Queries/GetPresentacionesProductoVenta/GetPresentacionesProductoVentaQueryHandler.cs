using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Queries.GetPresentacionesProductoVenta;

public class GetPresentacionesProductoVentaQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetPresentacionesProductoVentaQuery, Result<List<PresentacionProductoVentaDto>>>
{
    public async Task<Result<List<PresentacionProductoVentaDto>>> Handle(GetPresentacionesProductoVentaQuery request, CancellationToken cancellationToken)
    {
        var presentaciones = await context.PresentacionesVenta
            .AsNoTracking()
            .Where(x => x.ProductoId == request.ProductoId && x.Producto != null)
            .OrderBy(x => x.Descripcion)
            .Select(x => new PresentacionProductoVentaDto(x.Id, x.Descripcion, x.CantidadUnidadesBase, x.PrecioVenta))
            .ToListAsync(cancellationToken);

        return Result.Success(presentaciones);
    }
}
