using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Pacientes.Queries.GetPacienteById;

public record PacienteDto(Guid Id, string DocIdentidadGub, string NombreApellidos, DateOnly FechaNacimiento, string? ContactoPrimario);

public record GetPacienteByIdQuery(Guid Id) : IRequest<Result<PacienteDto>>;
