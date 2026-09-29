using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.DeleteCuenta;

public record DeleteCuentaCommand(Guid Id) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.CuentaEliminar, AuditTablas.Cuentas, Id);
}
