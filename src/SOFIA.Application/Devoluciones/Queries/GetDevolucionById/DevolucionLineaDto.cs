namespace SOFIA.Application.Devoluciones.Queries.GetDevolucionById;

/// <summary>One returned sale line; Destino is the enum name (Reingreso_Venta or Cuarentena_DIGEMID) and Subtotal is CantidadDevuelta times PrecioUnitario.</summary>
public record DevolucionLineaDto(
    Guid Id,
    Guid DetalleVentaId,
    string ProductoNombre,
    string NumeroLote,
    string? Presentacion,
    decimal CantidadDevuelta,
    decimal PrecioUnitario,
    decimal Subtotal,
    string Destino
);
