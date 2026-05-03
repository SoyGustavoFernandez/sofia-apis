namespace SOFIA.Application.Laboratorios;

public record LaboratorioDto(
    Guid Id,
    string NombreCompania,
    string? CodigoIdentificador);
