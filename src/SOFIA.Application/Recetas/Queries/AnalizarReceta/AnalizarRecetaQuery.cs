using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Queries.AnalizarReceta;

public record AnalizarRecetaQuery(Stream ImagenStream, string? EspecialidadContexto) : IRequest<Result<List<ItemSugeridoDto>>>;

public class ItemSugeridoDto
{
    public Guid? MedicamentoId { get; set; }
    public string NombreOficial { get; set; } = string.Empty;
    public double ConfianzaFinal { get; set; }
    public string TipoSugerencia { get; set; } = string.Empty;
    public string RazonSugerencia { get; set; } = string.Empty;
    public bool ValidadoPorDigemid { get; set; }
}
