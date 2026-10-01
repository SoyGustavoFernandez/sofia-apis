using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Pacientes.Commands.CreatePaciente;

public record CreatePacienteCommand(string? TipoDocumento, string DocIdentidadGub, string NombreApellidos, DateOnly FechaNacimiento, string? ContactoPrimario) : ICommand<Guid>;
