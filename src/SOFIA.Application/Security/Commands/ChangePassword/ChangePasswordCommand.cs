using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Security.Commands.ChangePassword;

public record ChangePasswordCommand(Guid CuentaId, string CurrentPassword, string NewPassword) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.ClaveCambiar, AuditTablas.Cuentas, CuentaId);
}
