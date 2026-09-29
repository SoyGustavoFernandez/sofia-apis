using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.SeriesFiscales.Commands.UpdateSerieFiscal;

public record UpdateSerieFiscalCommand(
    Guid Id,
    Guid SucursalId,
    TipoComprobante TipoComprobante,
    string PrefijoSerie,
    int CorrelativoActual,
    string EstadoSerie) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.SerieActualizar, AuditTablas.SeriesFiscales, Id, $"tipo: {TipoComprobante}; serie: {PrefijoSerie}; correlativo: {CorrelativoActual}; estado: {EstadoSerie}");
}
