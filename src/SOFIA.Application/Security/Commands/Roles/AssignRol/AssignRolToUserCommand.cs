using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.AssignRol;

public record AssignRolToUserCommand(Guid CuentaId, Guid RolId) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.RolAsignarCuenta, AuditTablas.Cuentas, CuentaId, $"rol: {RolId}");
}
