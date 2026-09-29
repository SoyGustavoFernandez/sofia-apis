using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Devoluciones.Commands.ProcesarDevolucion;

public record DevolucionDetalleDto(Guid DetalleVentaId, decimal CantidadDevuelta, DestinoDevolucion DestinoFisicoLogico);

// ComprobanteOrigenId carries the sale id; the handler resolves the sale's issued boleta/factura from it
public record ProcesarDevolucionCommand(
    Guid ComprobanteOrigenId,
    string MotivoSunatCatalogo,
    string SustentoDescriptivo,
    List<DevolucionDetalleDto> Detalles,
    MetodoPago MetodoReembolso = MetodoPago.Efectivo
) : ICommand<Guid>, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        resultValue is Guid id ? new(AuditEventos.DevolucionProcesar, AuditTablas.Devoluciones, id, $"venta: {ComprobanteOrigenId}; items: {Detalles.Count}; reembolso: {MetodoReembolso}") : null;
}
