using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Citas.Commands.ProgramarCita;

public record ProgramarCitaCommand(Guid PacienteId, Guid ServicioId, DateTimeOffset FechaHora, bool AceptaRecordatorio) : ICommand<Guid>;
