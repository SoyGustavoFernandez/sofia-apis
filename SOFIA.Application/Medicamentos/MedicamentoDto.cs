namespace SOFIA.Application.Medicamentos;

public record MedicamentoDto(
    Guid Id,
    string CodigoNacional,
    string NombreComercial,
    Guid LaboratorioId,
    string LaboratorioNombre,
    Guid UnidadBaseId,
    string UnidadBaseNombre,
    string CondicionVenta);
