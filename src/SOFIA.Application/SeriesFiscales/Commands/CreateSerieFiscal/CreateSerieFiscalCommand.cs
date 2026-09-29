using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.SeriesFiscales.Commands.CreateSerieFiscal;

public record CreateSerieFiscalCommand(
    Guid SucursalId,
    TipoComprobante TipoComprobante,
    string PrefijoSerie,
    int CorrelativoActual,
    string EstadoSerie) : ICommand<Guid>, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        resultValue is Guid id ? new(AuditEventos.SerieCrear, AuditTablas.SeriesFiscales, id, $"tipo: {TipoComprobante}; serie: {PrefijoSerie}; correlativo: {CorrelativoActual}; estado: {EstadoSerie}") : null;
}
