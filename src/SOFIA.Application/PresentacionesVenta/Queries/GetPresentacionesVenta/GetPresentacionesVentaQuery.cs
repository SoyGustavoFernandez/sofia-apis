using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.PresentacionesVenta.Queries.GetPresentacionesVenta;

public record GetPresentacionesVentaQuery : IRequest<Result<PaginatedList<PresentacionVentaDto>>>
{
    public Guid? ProductoId { get; init; }
    public string? ProductoNombre { get; init; }
    public string? Descripcion { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
