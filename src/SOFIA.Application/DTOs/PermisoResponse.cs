namespace SOFIA.Application.DTOs;

public record PermisoResponse(
    Guid Id,
    string ModuloSistema,
    string Accion);
