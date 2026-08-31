using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.IngredientesActivos.Commands.CreateIngredienteActivo;

public record CreateIngredienteActivoCommand(string DenominacionDci, string CodigoAtc) : ICommand<Guid>;
