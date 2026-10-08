namespace SOFIA.Application.Proveedores.Queries.GetPreciosProveedor;

public record PrecioProveedorDto(Guid Id, Guid ProductoId, string ProductoNombre, decimal CostoPorUnidadBase, DateTime FechaInicioVigencia, DateTime? FechaFinVigencia, bool Vigente);
