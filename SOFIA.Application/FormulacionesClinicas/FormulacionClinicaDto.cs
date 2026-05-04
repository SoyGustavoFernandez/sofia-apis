namespace SOFIA.Application.FormulacionesClinicas;

public record FormulacionClinicaDto
{
    public Guid Id { get; init; }
    public Guid ProductoId { get; init; }
    public string? ProductoNombre { get; init; }
    public Guid IngredienteId { get; init; }
    public string? IngredienteNombre { get; init; }
    public decimal ConcentracionDosis { get; init; }
    public string UnidadDosisClinica { get; init; } = string.Empty;
    public string? CodigoTeOrange { get; init; }
}
