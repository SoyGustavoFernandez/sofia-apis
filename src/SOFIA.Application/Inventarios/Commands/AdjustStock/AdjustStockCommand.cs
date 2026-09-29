using System.Globalization;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Inventarios.Commands.AdjustStock;

public record AdjustStockCommand(Guid Id, decimal NuevaCantidad) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.StockAjustar, AuditTablas.Inventario, Id, $"nuevaCantidad: {NuevaCantidad.ToString(CultureInfo.InvariantCulture)}");
}
