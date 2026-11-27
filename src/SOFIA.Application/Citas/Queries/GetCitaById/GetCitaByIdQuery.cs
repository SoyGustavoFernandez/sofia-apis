using MediatR;
using SOFIA.Application.Citas.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Citas.Queries.GetCitaById;

public record GetCitaByIdQuery(Guid Id) : IRequest<Result<CitaDto>>;
