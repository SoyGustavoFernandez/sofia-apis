using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Pacientes.Queries.GetPacientes;

public record PacienteDto(Guid Id, string DocIdentidadGub, string NombreApellidos, DateOnly FechaNacimiento, string? ContactoPrimario);

public record GetPacientesQuery : IRequest<Result<PaginatedList<PacienteDto>>>
{
    public string? DocIdentidadGub { get; init; }
    public string? NombreApellidos { get; init; }
    public DateOnly? FechaNacimientoDesde { get; init; }
    public DateOnly? FechaNacimientoHasta { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
