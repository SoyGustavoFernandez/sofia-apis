using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.JerarquiasUoM.Commands.CreateJerarquiaUoM;

public record CreateJerarquiaUoMCommand(
    Guid ProductoId,
    Guid UnidadMayorId,
    Guid UnidadMenorId,
    decimal Multiplicador) : ICommand<Guid>;
