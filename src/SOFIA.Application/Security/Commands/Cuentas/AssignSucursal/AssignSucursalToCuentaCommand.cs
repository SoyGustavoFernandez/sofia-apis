using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.AssignSucursal;

public record AssignSucursalToCuentaCommand(Guid CuentaId, Guid SucursalId) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.CuentaSucursalAsignar, AuditTablas.Cuentas, CuentaId, $"sucursal: {SucursalId}");
}
