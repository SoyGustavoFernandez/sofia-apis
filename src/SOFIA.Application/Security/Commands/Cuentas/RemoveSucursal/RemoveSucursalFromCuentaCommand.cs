using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.RemoveSucursal;

public record RemoveSucursalFromCuentaCommand(Guid CuentaId, Guid SucursalId) : ICommand;
