using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.RemoveRol;

public record RemoveRolFromUserCommand(Guid CuentaId, Guid RolId) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.RolQuitarCuenta, AuditTablas.Cuentas, CuentaId, $"rol: {RolId}");
}
