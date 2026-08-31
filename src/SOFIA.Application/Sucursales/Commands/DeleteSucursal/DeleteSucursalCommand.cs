using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Commands.DeleteSucursal;

public record DeleteSucursalCommand(Guid Id) : ICommand;
