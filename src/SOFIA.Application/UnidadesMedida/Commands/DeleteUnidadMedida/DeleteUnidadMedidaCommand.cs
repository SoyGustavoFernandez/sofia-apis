using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Commands.DeleteUnidadMedida;

public record DeleteUnidadMedidaCommand(Guid Id) : ICommand;
