using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Queries.GetPreciosProveedor;

public record GetPreciosProveedorQuery(Guid ProveedorId, int PageNumber = 1, int PageSize = 10) : IRequest<Result<PaginatedList<PrecioProveedorDto>>>;
