namespace SOFIA.Application.Security;

public record SucursalAsignadaDto(Guid Id, string Nombre);

public record CuentaDto(
    Guid Id,
    Guid EmpleadoId,
    string NombreEmpleado,
    string NombreUsuario,
    bool CuentaActiva,
    bool RequiereCambioClave,
    int IntentosFallidos,
    DateTimeOffset? BloqueadoHasta,
    DateTimeOffset CreatedAt,
    IReadOnlyList<RolResponse> Roles,
    IReadOnlyList<SucursalAsignadaDto> Sucursales);
