using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Pacientes.Queries.GetPacientes;

public record PacienteDto(Guid Id, string DocIdentidadGub, string NombreApellidos, DateOnly FechaNacimiento, string? ContactoPrimario);

public record GetPacientesQuery : IRequest<Result<PaginatedList<PacienteDto>>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
