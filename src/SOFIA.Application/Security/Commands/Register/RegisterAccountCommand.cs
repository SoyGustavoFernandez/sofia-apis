using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Security.Commands.Register;

public record RegisterAccountCommand(
    Guid EmpleadoId,
    string NombreUsuario,
    string Password) : ICommand<Guid>, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        resultValue is Guid id ? new(AuditEventos.CuentaRegistrar, AuditTablas.Cuentas, id, $"empleado: {EmpleadoId}") : null;
}
