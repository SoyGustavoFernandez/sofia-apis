using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Empresas.Commands.ActivarSuscripcionEmpresa;

public record ActivarSuscripcionEmpresaCommand : ICommand, IAuditableCommand
{
    public Guid Id { get; init; }
    public DateTimeOffset FechaVencimiento { get; init; }

    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.EmpresaActivar, AuditTablas.Empresas, Id, $"Vence: {FechaVencimiento.UtcDateTime:yyyy-MM-dd}");
}
