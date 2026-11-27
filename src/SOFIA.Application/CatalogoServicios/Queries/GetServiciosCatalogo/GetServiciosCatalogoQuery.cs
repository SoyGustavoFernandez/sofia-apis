using MediatR;
using SOFIA.Application.CatalogoServicios.Common;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.CatalogoServicios.Queries.GetServiciosCatalogo;

public record GetServiciosCatalogoQuery : IRequest<Result<PaginatedList<ServicioCatalogoDto>>>
{
    public string? Nombre { get; init; }
    public decimal? PrecioMin { get; init; }
    public decimal? PrecioMax { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
