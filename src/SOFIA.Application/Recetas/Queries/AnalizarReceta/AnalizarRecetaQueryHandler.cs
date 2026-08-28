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
            await ProcessMedicamentoAsync(itemIA, resultadosFinales, cancellationToken);
        }

        return Result.Success(resultadosFinales);
    }

    private async Task ProcessMedicamentoAsync(MedicamentoInterpretadoDto itemIA, List<ItemSugeridoDto> resultados, CancellationToken cancellationToken)
    {
        var concentracion = itemIA.ConcentracionDetectada;
        if (string.Equals(concentracion, "null", StringComparison.OrdinalIgnoreCase))
        {
            concentracion = null;
        }

        var terminoOriginalIA = string.Join(" ", new[] { itemIA.NombreDetectado, concentracion }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

        var candidatosDigemid = await _buscadorService.BuscarEnDigemidAsync(itemIA.NombreDetectado, 5, cancellationToken);
        var terminoParaBusquedaInterna = terminoOriginalIA;
        var encontradoEnDigemid = false;
        string? nombreDigemidOficial = null;
        var similarityScore = 1.0;

        if (candidatosDigemid.Count != 0)
        {
            var mejorCoincidencia = candidatosDigemid[0];
            nombreDigemidOficial = mejorCoincidencia.NomProd;
            terminoParaBusquedaInterna = $"{mejorCoincidencia.NomProd} {mejorCoincidencia.Concent}".Trim();
            encontradoEnDigemid = true;
            similarityScore = 0.9;
        }

        var confianzaFinal = (itemIA.NivelConfianza * 0.6) + (similarityScore * 0.4);

        var productoDb = await _buscadorService.BuscarMejorCoincidenciaAsync(terminoParaBusquedaInterna, cancellationToken);

        if (productoDb == null && encontradoEnDigemid)
        {
            productoDb = await _buscadorService.BuscarMejorCoincidenciaAsync(terminoOriginalIA, cancellationToken);
        }

        AgregarResultado(resultados, productoDb, nombreDigemidOficial ?? terminoOriginalIA, confianzaFinal, "Receta",
            encontradoEnDigemid ? $"Validado por DIGEMID ({nombreDigemidOficial})" : "Detectado en receta", encontradoEnDigemid);

        await AddSugerenciasAsync(itemIA, resultados, cancellationToken);
    }

    private async Task AddSugerenciasAsync(MedicamentoInterpretadoDto itemIA, List<ItemSugeridoDto> resultados, CancellationToken cancellationToken)
    {
        if (itemIA.Sugerencias == null)
        {
            return;
        }

        foreach (var sugerencia in itemIA.Sugerencias)
        {
            var productoSugeridoDb = await _buscadorService.BuscarMejorCoincidenciaAsync(sugerencia, cancellationToken);
            if (productoSugeridoDb != null)
            {
                AgregarResultado(resultados, productoSugeridoDb, sugerencia, 0.70, "Recomendacion",
                    $"Sugerido por compra de {itemIA.NombreDetectado}", false);
            }
        }
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
