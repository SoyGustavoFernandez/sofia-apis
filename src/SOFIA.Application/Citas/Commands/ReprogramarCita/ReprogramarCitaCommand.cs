using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Citas.Commands.ReprogramarCita;

public record ReprogramarCitaCommand(Guid Id, Guid ServicioId, DateTimeOffset FechaHora, bool AceptaRecordatorio) : ICommand;
