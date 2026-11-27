using MediatR;
using SOFIA.Application.CatalogoServicios.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.CatalogoServicios.Queries.GetServicioCatalogoById;

public record GetServicioCatalogoByIdQuery(Guid Id) : IRequest<Result<ServicioCatalogoDto>>;
