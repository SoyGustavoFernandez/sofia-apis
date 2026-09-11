using SOFIA.Application.Common.Models;

namespace SOFIA.Application.Common.Interfaces;

public class MedicamentoInterpretadoDto
{
    public string NombreDetectado { get; set; } = string.Empty;
    public string? ConcentracionDetectada { get; set; }
    public int? CantidadSugerida { get; set; }
    public List<string>? Sugerencias { get; set; }
    public double NivelConfianza { get; set; }
}

public interface IRecetaAnalyzer
{
    Task<List<MedicamentoInterpretadoDto>> InterpretarRecetaAsync(Stream imagenStream, string? especialidadContexto, CancellationToken cancellationToken = default);
}
