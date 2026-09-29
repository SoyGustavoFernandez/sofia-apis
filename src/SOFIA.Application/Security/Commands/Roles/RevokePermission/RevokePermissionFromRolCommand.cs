using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.RevokePermission;

public record RevokePermissionFromRolCommand(Guid PermisoId) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.PermisoRevocar, AuditTablas.Permisos, PermisoId);
}
