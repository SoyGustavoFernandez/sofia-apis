using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Empresas.Commands.ExtenderPruebaEmpresa;

public record ExtenderPruebaEmpresaCommand : ICommand, IAuditableCommand
{
    public Guid Id { get; init; }
    public DateTimeOffset FechaVencimiento { get; init; }

    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.EmpresaExtenderPrueba, AuditTablas.Empresas, Id, $"Vence: {FechaVencimiento.UtcDateTime:yyyy-MM-dd}");
}
