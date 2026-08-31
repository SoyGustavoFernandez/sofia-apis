using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Commands.DeleteLote;

public record DeleteLoteInventarioCommand(Guid Id) : ICommand;
