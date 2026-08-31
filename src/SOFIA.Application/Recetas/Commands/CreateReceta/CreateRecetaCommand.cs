using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Commands.CreateReceta;

public record CreateRecetaCommand(Guid ClienteId, Guid MedicoId, DateOnly FechaExpedicion, int RepeticionesMax = 0, string? IndicacionesUso = null) : ICommand<Guid>;
