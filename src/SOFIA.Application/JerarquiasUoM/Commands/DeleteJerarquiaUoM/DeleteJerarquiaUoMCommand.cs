using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Commands.DeleteJerarquiaUoM;

public record DeleteJerarquiaUoMCommand(Guid Id) : ICommand;
