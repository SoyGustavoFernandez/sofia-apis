using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Commands.DeleteDigemidProducto;

public record DeleteDigemidProductoCommand(Guid Id) : ICommand;
