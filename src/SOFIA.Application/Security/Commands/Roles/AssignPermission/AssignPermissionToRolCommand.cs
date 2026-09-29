using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.AssignPermission;

public record AssignPermissionToRolCommand(Guid RolId, string ModuloSistema, string Accion) : ICommand<Guid>, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        resultValue is Guid id ? new(AuditEventos.PermisoAsignar, AuditTablas.Permisos, id, $"rol: {RolId}; permiso: {ModuloSistema}:{Accion}") : null;
}
