using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Queries.GetRecetas;

public record RecetaResumenDto(
    Guid Id,
    Guid ClienteId,
    string ClienteNombre,
    Guid MedicoId,
    string MedicoNombre,
    DateOnly FechaExpedicion,
    int RepeticionesMax,
    string? IndicacionesUso);

public record GetRecetasQuery(
    Guid? ClienteId,
    Guid? MedicoId,
    DateTime? FechaInicio,
    DateTime? FechaFin,
    int PageNumber = 1,
    int PageSize = 10) : IRequest<Result<PaginatedList<RecetaResumenDto>>>;
