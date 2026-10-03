using SOFIA.Domain.Enums;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

public record VentaMetodoPagoDto(MetodoPago Metodo, decimal Monto);
