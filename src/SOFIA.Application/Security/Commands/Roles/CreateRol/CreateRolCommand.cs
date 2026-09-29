using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Roles.CreateRol;

public record CreateRolCommand(string NombreRol, string? Descripcion, int NivelJerarquia) : ICommand<Guid>, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        resultValue is Guid id ? new(AuditEventos.RolCrear, AuditTablas.Roles, id, $"nivel: {NivelJerarquia}") : null;
}
