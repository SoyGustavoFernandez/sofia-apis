using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Devoluciones.Queries.GetDevolucionById;

public record GetDevolucionByIdQuery(Guid Id) : IRequest<Result<DevolucionConDetalleDto>>;
