using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Queries.AnalizarReceta;

public record AnalizarRecetaQuery(Stream ImagenStream, string? EspecialidadContexto) : IRequest<Result<List<ItemSugeridoDto>>>;

public class EquivalenteDto
{
    public Guid MedicamentoId { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string? Laboratorio { get; set; }
    public decimal StockDisponible { get; set; }
    public bool TieneStock { get; set; }
}

public class ItemSugeridoDto
{
    public Guid? MedicamentoId { get; set; }
    public string NombreOficial { get; set; } = string.Empty;
    public string? NombreDetectado { get; set; }
    public double ConfianzaFinal { get; set; }
    public string TipoSugerencia { get; set; } = string.Empty;
    public string RazonSugerencia { get; set; } = string.Empty;
    public bool ValidadoPorDigemid { get; set; }
    public int? CantidadSugerida { get; set; }
    public decimal StockDisponible { get; set; }
    public bool TieneStock { get; set; }
    public List<EquivalenteDto> Equivalentes { get; set; } = [];
}
