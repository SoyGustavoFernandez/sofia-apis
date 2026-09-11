using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Queries.GetRecetaById;

public record RecetaDto(
    Guid Id,
    Guid ClienteId,
    string ClienteNombre,
    Guid MedicoId,
    string MedicoNombre,
    DateOnly FechaExpedicion,
    int RepeticionesMax,
    string? IndicacionesUso);

public record GetRecetaByIdQuery(Guid Id) : IRequest<Result<RecetaDto>>;
