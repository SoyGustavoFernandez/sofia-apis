namespace SOFIA.Application.Dashboard.Queries.GetDashboardCaja;

public record SesionAbiertaDto(Guid Id, string SucursalNombre, string EmpleadoNombre, DateTime FechaHoraApertura, decimal MontoAperturaEfectivo);
