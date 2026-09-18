using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.PresentacionesVenta.Queries.GetPresentacionesVenta;

public class GetPresentacionesVentaQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPresentacionesVentaQuery, Result<PaginatedList<PresentacionVentaDto>>>
{
    public async Task<Result<PaginatedList<PresentacionVentaDto>>> Handle(GetPresentacionesVentaQuery request, CancellationToken cancellationToken)
    {
        // Producto is required; a row whose principal was soft-deleted is invalid data and is excluded.
        var query = context.PresentacionesVenta
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Producto != null);

        if (request.ProductoId.HasValue)
        {
            query = query.Where(x => x.ProductoId == request.ProductoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ProductoNombre))
        {
            var term = request.ProductoNombre.ToLower();
            query = query.Where(x => x.Producto!.NombreComercial.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.Descripcion))
        {
            var term = request.Descripcion.ToLower();
            query = query.Where(x => x.Descripcion.ToLower().Contains(term));
        }

        var paginatedList = await PaginatedList<PresentacionVentaDto>.CreateAsync(
            query.OrderBy(x => x.Producto!.NombreComercial).ThenBy(x => x.Descripcion)
                .Select(x => new PresentacionVentaDto(
                    x.Id,
                    x.ProductoId,
                    x.Producto!.NombreComercial,
                    x.UnidadVentaId,
                    x.Descripcion,
                    x.CantidadUnidadesBase,
                    x.PrecioVenta)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
