using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Queries.GetLotesDisponiblesVenta;

// POS lookup: lots the caller's own sucursal can sell for one product, matched by id or by name.
public record GetLotesDisponiblesVentaQuery : IRequest<Result<List<LoteDisponibleVentaDto>>>
{
    public Guid? ProductoId { get; init; }
    public string? ProductoNombre { get; init; }
}
