using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Plataforma.Commands.GuardarConfiguracionPago;

public record GuardarConfiguracionPagoCommand : ICommand<Guid>, IAuditableCommand
{
    public string WhatsappNumero { get; init; } = string.Empty;
    public string? YapeNumero { get; init; }
    public string? Instrucciones { get; init; }

    // Null keeps the QR already stored
    public byte[]? QrImagen { get; init; }

    // Replacing the QR redirects every payment, so the audit records whether it changed
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        resultValue is Guid id
            ? new(AuditEventos.PagoConfigGuardar, AuditTablas.PlataformaConfiguracionPago, id, QrImagen is null ? "QR sin cambio" : "QR reemplazado")
            : null;
}
