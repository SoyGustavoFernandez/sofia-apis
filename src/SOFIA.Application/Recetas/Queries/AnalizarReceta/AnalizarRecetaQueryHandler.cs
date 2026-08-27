using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Recetas.Queries.AnalizarReceta;

public class AnalizarRecetaQueryHandler(IRecetaAnalyzer recetaAnalyzer, IBuscadorService buscadorService) : IRequestHandler<AnalizarRecetaQuery, Result<List<ItemSugeridoDto>>>
{
    private readonly IRecetaAnalyzer _recetaAnalyzer = recetaAnalyzer;
    private readonly IBuscadorService _buscadorService = buscadorService;

    public async Task<Result<List<ItemSugeridoDto>>> Handle(AnalizarRecetaQuery request, CancellationToken cancellationToken)
    {
        var medicamentosInterpretados = await _recetaAnalyzer.InterpretarRecetaAsync(
            request.ImagenStream,
            request.EspecialidadContexto,
            cancellationToken
        );

        var resultadosFinales = new List<ItemSugeridoDto>();

        foreach (var itemIA in medicamentosInterpretados)
        {
            var concentracion = itemIA.ConcentracionDetectada;
            if (string.Equals(concentracion, "null", StringComparison.OrdinalIgnoreCase))
            {
                concentracion = null;
            }

            var terminoOriginalIA = string.Join(" ", new[] { itemIA.NombreDetectado, concentracion }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

            // 1. Cruzar con DIGEMID
            var candidatosDigemid = await _buscadorService.BuscarEnDigemidAsync(itemIA.NombreDetectado, 5, cancellationToken);
            var terminoParaBusquedaInterna = terminoOriginalIA;
            var encontradoEnDigemid = false;
            string? nombreDigemidOficial = null;
            var similarityScore = 1.0;

            if (candidatosDigemid.Any())
            {
                // BuscadorService already returns results sorted by similarity (Levenshtein)
                var mejorCoincidencia = candidatosDigemid.First();

                nombreDigemidOficial = mejorCoincidencia.NomProd;
                terminoParaBusquedaInterna = $"{mejorCoincidencia.NomProd} {mejorCoincidencia.Concent}".Trim();
                encontradoEnDigemid = true;

                // Simple local similarity score
                similarityScore = 0.9; // High base: already filtered by Levenshtein in the search service
            }

            // 2. Calcular confianza final
            var confianzaFinal = (itemIA.NivelConfianza * 0.6) + (similarityScore * 0.4);

            // 3. Buscar en Inventario Interno con el término
            var productoDb = await _buscadorService.BuscarMejorCoincidenciaAsync(terminoParaBusquedaInterna, cancellationToken);

            // If not found by DIGEMID name, fall back to the original AI-provided name
            if (productoDb == null && encontradoEnDigemid)
            {
                productoDb = await _buscadorService.BuscarMejorCoincidenciaAsync(terminoOriginalIA, cancellationToken);
            }

            AgregarResultado(
                resultadosFinales,
                productoDb,
                nombreDigemidOficial ?? terminoOriginalIA,
                confianzaFinal,
                "Receta",
                encontradoEnDigemid ? $"Validado por DIGEMID ({nombreDigemidOficial})" : "Detectado en receta",
                encontradoEnDigemid
            );

            // Cross-selling suggestions
            if (itemIA.Sugerencias != null)
            {
                foreach (var sugerencia in itemIA.Sugerencias)
                {
                    var productoSugeridoDb = await _buscadorService.BuscarMejorCoincidenciaAsync(sugerencia, cancellationToken);
                    if (productoSugeridoDb != null)
                    {
                        AgregarResultado(
                            resultadosFinales,
                            productoSugeridoDb,
                            sugerencia,
                            0.70,
                            "Recomendacion",
                            $"Sugerido por compra de {itemIA.NombreDetectado}",
                            false
                        );
                    }
                }
            }
        }

        return Result.Success(resultadosFinales);
    }

    private static void AgregarResultado(
        List<ItemSugeridoDto> resultados,
        Medicamento? productoDb,
        string nombreOficial,
        double confianza,
        string tipo,
        string razon,
        bool validado) => resultados.Add(new ItemSugeridoDto
        {
            MedicamentoId = productoDb?.Id,
            NombreOficial = productoDb?.NombreComercial ?? nombreOficial,
            ConfianzaFinal = Math.Round(confianza, 2),
            TipoSugerencia = tipo,
            RazonSugerencia = razon,
            ValidadoPorDigemid = validado
        });
}
