namespace SOFIA.Application.PresentacionesVenta;

public record PresentacionVentaDto(
    Guid Id,
    Guid ProductoId,
    string ProductoNombre,
    Guid UnidadVentaId,
    string Descripcion,
    decimal CantidadUnidadesBase,
    decimal PrecioVenta);
