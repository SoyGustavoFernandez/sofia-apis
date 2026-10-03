namespace SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

// Desde is the start of that local day, so the client can label it
public record VentasDiaDto(DateTimeOffset Desde, decimal Monto, int Comprobantes);
