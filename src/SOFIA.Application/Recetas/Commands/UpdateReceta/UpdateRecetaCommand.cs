using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Commands.UpdateReceta;

public record UpdateRecetaCommand(
    Guid Id,
    Guid ClienteId,
    Guid MedicoId,
    DateOnly FechaExpedicion,
    int RepeticionesMax,
    string? IndicacionesUso) : ICommand<Guid>;
