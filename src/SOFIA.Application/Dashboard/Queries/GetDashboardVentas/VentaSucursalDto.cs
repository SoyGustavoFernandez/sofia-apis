namespace SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

public record VentaSucursalDto(Guid SucursalId, string Nombre, decimal Monto, int Comprobantes);
