namespace SOFIA.Application.Security;

public record PermisoResponse(
    Guid Id,
    string ModuloSistema,
    string Accion);
