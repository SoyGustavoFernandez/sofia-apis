using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.UpdateRol;

public record UpdateRolCommand(Guid Id, string? Descripcion, int NivelJerarquia) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.RolActualizar, AuditTablas.Roles, Id, $"nivel: {NivelJerarquia}");
}
