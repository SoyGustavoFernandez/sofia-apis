using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Inventarios.Commands.CreateLote;

public record CreateLoteInventarioCommand(
    Guid ProductoId,
    string NumeroLoteMfr,
    DateTimeOffset? FechaFabricacion,
    DateTimeOffset FechaCaducidad) : ICommand<Guid>;
