using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Queries.GetPresentacionesProductoVenta;

// POS lookup: the sale presentations of one product, readable with the sales permission alone.
public record GetPresentacionesProductoVentaQuery(Guid ProductoId) : IRequest<Result<List<PresentacionProductoVentaDto>>>;
