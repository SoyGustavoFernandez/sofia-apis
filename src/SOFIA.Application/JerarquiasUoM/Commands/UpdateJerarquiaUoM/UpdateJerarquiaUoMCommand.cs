using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Commands.UpdateJerarquiaUoM;

public record UpdateJerarquiaUoMCommand(
    Guid Id,
    Guid ProductoId,
    Guid UnidadMayorId,
    Guid UnidadMenorId,
    decimal Multiplicador) : ICommand;
