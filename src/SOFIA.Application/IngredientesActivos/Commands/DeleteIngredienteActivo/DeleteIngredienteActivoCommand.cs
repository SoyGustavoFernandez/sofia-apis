using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.IngredientesActivos.Commands.DeleteIngredienteActivo;

public record DeleteIngredienteActivoCommand(Guid Id) : ICommand;
