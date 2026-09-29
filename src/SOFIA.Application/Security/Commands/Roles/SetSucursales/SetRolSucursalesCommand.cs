using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Roles.SetSucursales;

public record SetRolSucursalesCommand(Guid RolId, List<Guid> SucursalIds) : IRequest<Result>, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.RolSucursales, AuditTablas.Roles, RolId, $"sucursales: {SucursalIds.Count}");
}
