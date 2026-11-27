using SOFIA.Domain.Enums;

namespace SOFIA.Domain.Entities;

// What is written down when a service is performed
public sealed record AtencionDatos(
    OrigenMedicamento? Origen = null,
    Guid? LoteId = null,
    string? MedicamentoTraido = null,
    string? Dosis = null,
    string? Resultado = null,
    Guid? VentaPreviaId = null);
