using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.AssignSucursal;

public record AssignSucursalToCuentaCommand(Guid CuentaId, Guid SucursalId) : ICommand;
