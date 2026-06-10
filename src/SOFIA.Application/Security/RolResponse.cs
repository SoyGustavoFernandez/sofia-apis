namespace SOFIA.Application.Security;

public record RolResponse(
    Guid Id,
    string NombreRol,
    string? Descripcion,
    int NivelJerarquia,
    DateTimeOffset CreatedAt);
