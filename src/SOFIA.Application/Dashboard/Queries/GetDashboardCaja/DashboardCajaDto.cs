namespace SOFIA.Application.Dashboard.Queries.GetDashboardCaja;

public record DashboardCajaDto(MiSesionDto? MiSesion, List<SesionAbiertaDto> SesionesAbiertas);
