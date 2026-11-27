using MediatR;
using SOFIA.Application.Citas.Common;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Citas.Queries.GetCitas;

// Desde is inclusive and Hasta exclusive, both sent by the screen as instants of the user's day
public record GetCitasQuery : IRequest<Result<PaginatedList<CitaDto>>>
{
    public DateTimeOffset? Desde { get; init; }
    public DateTimeOffset? Hasta { get; init; }
    public EstadoCita? Estado { get; init; }
    public string? PacienteNombre { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
