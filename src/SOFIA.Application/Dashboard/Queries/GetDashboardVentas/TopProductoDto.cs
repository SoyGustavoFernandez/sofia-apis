namespace SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

// Unidades are base units
public record TopProductoDto(Guid ProductoId, string Nombre, decimal Unidades, decimal Monto);
