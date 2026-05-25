namespace SOFIA.Application.DTOs;

public record RolResponse(
    Guid Id,
    string NombreRol,
    string? Descripcion,
    int NivelJerarquia,
    DateTimeOffset CreatedAt);
