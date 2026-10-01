namespace SOFIA.Application.Ventas.Queries.GetPresentacionesProductoVenta;

public record PresentacionProductoVentaDto(
    Guid Id,
    string Descripcion,
    decimal CantidadUnidadesBase,
    decimal PrecioVenta);
