using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Empresas.Commands.SuspenderEmpresa;

public record SuspenderEmpresaCommand(Guid Id) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.EmpresaSuspender, AuditTablas.Empresas, Id);
}
