using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetLotesByProducto;

public record GetLotesByProductoQuery(Guid ProductoId) : IRequest<Result<PaginatedList<LoteInventarioDto>>>
{
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetLotesByProductoQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetLotesByProductoQuery, Result<PaginatedList<LoteInventarioDto>>>
{
    public async Task<Result<PaginatedList<LoteInventarioDto>>> Handle(GetLotesByProductoQuery request, CancellationToken cancellationToken)
    {
        var query = context.LotesInventario
            .AsNoTracking()
            .Include(x => x.Producto)
            .Where(x => x.ProductoId == request.ProductoId && !x.IsDeleted)
            .OrderByDescending(x => x.FechaCaducidad);

        var paginatedList = await PaginatedList<LoteInventarioDto>.CreateAsync(
            query.Select(x => new LoteInventarioDto(
                x.Id,
                x.ProductoId,
                x.Producto != null ? x.Producto.NombreComercial : "Unknown",
                x.NumeroLoteMfr,
                x.FechaFabricacion,
                x.FechaCaducidad)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
