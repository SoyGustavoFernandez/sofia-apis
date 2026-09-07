using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetStockPorSucursal;

public record GetStockPorSucursalQuery : IRequest<Result<PaginatedList<StockPorSucursalDto>>>
{
    public string? SucursalNombre { get; init; }
    public string? ProductoNombre { get; init; }
    public string? NumeroLote { get; init; }
    public DateTimeOffset? CaducidadDesde { get; init; }
    public DateTimeOffset? CaducidadHasta { get; init; }
    public decimal? CantidadMin { get; init; }
    public decimal? CantidadMax { get; init; }
    public bool SoloConStock { get; init; } = true;
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
