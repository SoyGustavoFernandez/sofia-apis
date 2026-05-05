using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetLotes;

public record GetLotesQuery : IRequest<Result<PaginatedList<LoteInventarioDto>>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetLotesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetLotesQuery, Result<PaginatedList<LoteInventarioDto>>>
{
    public async Task<Result<PaginatedList<LoteInventarioDto>>> Handle(GetLotesQuery request, CancellationToken cancellationToken)
    {
        var query = context.LotesInventario
            .AsNoTracking()
            .Include(x => x.Producto)
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.NumeroLoteMfr.ToLower().Contains(searchTerm) ||
                                     (x.Producto != null && x.Producto.NombreComercial.ToLower().Contains(searchTerm)));
        }

        var paginatedList = await PaginatedList<LoteInventarioDto>.CreateAsync(
            query.OrderByDescending(x => x.CreatedAt)
                 .Select(x => new LoteInventarioDto(
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
