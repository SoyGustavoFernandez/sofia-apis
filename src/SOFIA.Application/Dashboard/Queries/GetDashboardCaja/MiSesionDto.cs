namespace SOFIA.Application.Dashboard.Queries.GetDashboardCaja;

public record MiSesionDto(Guid Id, string SucursalNombre, DateTime FechaHoraApertura, decimal MontoAperturaEfectivo, decimal EfectivoEsperado);
