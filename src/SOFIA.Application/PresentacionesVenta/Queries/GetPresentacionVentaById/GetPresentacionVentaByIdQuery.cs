using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.PresentacionesVenta.Queries.GetPresentacionVentaById;

public record GetPresentacionVentaByIdQuery(Guid Id) : IRequest<Result<PresentacionVentaDto>>;
