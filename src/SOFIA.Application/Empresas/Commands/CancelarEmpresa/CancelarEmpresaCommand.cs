using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Empresas.Commands.CancelarEmpresa;

public record CancelarEmpresaCommand(Guid Id) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.EmpresaCancelar, AuditTablas.Empresas, Id);
}
