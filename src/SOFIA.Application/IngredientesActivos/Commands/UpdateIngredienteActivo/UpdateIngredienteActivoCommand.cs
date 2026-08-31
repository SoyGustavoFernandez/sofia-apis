using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.IngredientesActivos.Commands.UpdateIngredienteActivo;

public record UpdateIngredienteActivoCommand(Guid Id, string DenominacionDci, string CodigoAtc) : ICommand;
