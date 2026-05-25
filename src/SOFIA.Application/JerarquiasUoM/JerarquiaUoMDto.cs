namespace SOFIA.Application.JerarquiasUoM;

public record JerarquiaUoMDto(
    Guid Id,
    Guid ProductoId,
    string ProductoNombre,
    Guid UnidadMayorId,
    string UnidadMayorNombre,
    Guid UnidadMenorId,
    string UnidadMenorNombre,
    decimal Multiplicador);
