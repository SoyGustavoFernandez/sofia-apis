using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.SeriesFiscales.Commands.DeleteSerieFiscal;

public record DeleteSerieFiscalCommand(Guid Id) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.SerieEliminar, AuditTablas.SeriesFiscales, Id);
}
