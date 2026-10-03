namespace SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

public record DashboardVentasDto(
    bool MultiSucursal,
    bool EmpresaCompleta,
    VentasDiaDto Hoy,
    VentasDiaDto Ayer,
    decimal TicketPromedio,
    VentasDiaDto? Mias,
    List<VentaMetodoPagoDto> PorMetodoPago,
    VentasPendientesDto Pendientes,
    List<VentaSucursalDto> PorSucursal,
    List<VentasDiaDto> Tendencia7Dias,
    List<TopProductoDto> TopProductos);
