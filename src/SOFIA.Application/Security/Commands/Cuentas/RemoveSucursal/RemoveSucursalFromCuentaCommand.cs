using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.RemoveSucursal;

public record RemoveSucursalFromCuentaCommand(Guid CuentaId, Guid SucursalId) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.CuentaSucursalQuitar, AuditTablas.Cuentas, CuentaId, $"sucursal: {SucursalId}");
}
