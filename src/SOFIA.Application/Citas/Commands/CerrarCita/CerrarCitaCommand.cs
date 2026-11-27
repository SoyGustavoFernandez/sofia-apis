using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Citas.Commands.CerrarCita;

// Cancels an appointment or marks that the patient did not come
public record CerrarCitaCommand(Guid Id, EstadoCita Estado) : ICommand;
