using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Pacientes.Commands.CargaMasivaPacientes;

public record PacienteImportRow(string DocIdentidadGub, string NombreApellidos, string FechaNacimiento, string? ContactoPrimario);
public record CargaMasivaPacientesCommand(List<PacienteImportRow> Rows) : ICommand<int>;
