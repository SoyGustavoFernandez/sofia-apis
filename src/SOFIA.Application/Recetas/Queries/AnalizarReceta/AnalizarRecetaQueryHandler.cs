using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Recetas.Queries.AnalizarReceta;

public class AnalizarRecetaQueryHandler(
    IRecetaAnalyzer recetaAnalyzer,
    IBuscadorService buscadorService,
    IApplicationDbContext dbContext,
    ICurrentUser currentUser)
    : IRequestHandler<AnalizarRecetaQuery, Result<List<ItemSugeridoDto>>>
{
    private Guid? _sucursalId;

    public async Task<Result<List<ItemSugeridoDto>>> Handle(AnalizarRecetaQuery request, CancellationToken cancellationToken)
    {
        _sucursalId = Guid.TryParse(currentUser.SucursalId, out var sid) ? sid : null;

        var medicamentosInterpretados = await recetaAnalyzer.InterpretarRecetaAsync(
            request.ImagenStream,
            request.EspecialidadContexto,
            cancellationToken);

        var resultados = new List<ItemSugeridoDto>();
        foreach (var item in medicamentosInterpretados)
        {
            await ProcessMedicamentoAsync(item, resultados, cancellationToken);
        }

        return Result.Success(resultados);
    }

    private async Task ProcessMedicamentoAsync(
        MedicamentoInterpretadoDto itemIA,
        List<ItemSugeridoDto> resultados,
        CancellationToken ct)
    {
        var concentracion = string.Equals(itemIA.ConcentracionDetectada, "null", StringComparison.OrdinalIgnoreCase)
            ? null : itemIA.ConcentracionDetectada;

        var terminoOriginal = string.Join(" ",
            new[] { itemIA.NombreDetectado, concentracion }.Where(s => !string.IsNullOrWhiteSpace(s))).Trim();

        var candidatosDigemid = await buscadorService.BuscarEnDigemidAsync(itemIA.NombreDetectado, 5, ct);
        var terminoBusqueda = terminoOriginal;
        var encontradoEnDigemid = false;
        string? nombreDigemid = null;
        var similarityScore = 1.0;

        if (candidatosDigemid.Count != 0)
        {
            var mejor = candidatosDigemid[0];
            nombreDigemid = mejor.NomProd;
            terminoBusqueda = $"{mejor.NomProd} {mejor.Concent}".Trim();
            encontradoEnDigemid = true;
            similarityScore = 0.9;
        }

        var confianza = Math.Round((itemIA.NivelConfianza * 0.6) + (similarityScore * 0.4), 2);

        var productoDb = await buscadorService.BuscarMejorCoincidenciaAsync(terminoBusqueda, ct);
        if (productoDb == null && encontradoEnDigemid)
        {
            productoDb = await buscadorService.BuscarMejorCoincidenciaAsync(terminoOriginal, ct);
        }

        var stock = productoDb != null ? await GetStockAsync(productoDb.Id, ct) : 0;
        var equivalentes = productoDb != null ? await GetEquivalentesAsync(productoDb.Id, ct) : [];

        resultados.Add(new ItemSugeridoDto
        {
            MedicamentoId = productoDb?.Id,
            NombreOficial = productoDb?.NombreComercial ?? nombreDigemid ?? terminoOriginal,
            NombreDetectado = itemIA.NombreDetectado,
            ConfianzaFinal = confianza,
            TipoSugerencia = "Receta",
            RazonSugerencia = encontradoEnDigemid
                ? $"Validado por DIGEMID ({nombreDigemid})"
                : "Detectado en receta",
            ValidadoPorDigemid = encontradoEnDigemid,
            CantidadSugerida = itemIA.CantidadSugerida,
            StockDisponible = stock,
            TieneStock = stock > 0,
            Equivalentes = equivalentes,
        });

        await AddSugerenciasAsync(itemIA, resultados, ct);
    }

    private async Task AddSugerenciasAsync(
        MedicamentoInterpretadoDto itemIA,
        List<ItemSugeridoDto> resultados,
        CancellationToken ct)
    {
        if (itemIA.Sugerencias == null)
        {
            return;
        }

        foreach (var sugerencia in itemIA.Sugerencias)
        {
            var producto = await buscadorService.BuscarMejorCoincidenciaAsync(sugerencia, ct);
            if (producto == null)
            {
                continue;
            }

            var stock = await GetStockAsync(producto.Id, ct);
            resultados.Add(new ItemSugeridoDto
            {
                MedicamentoId = producto.Id,
                NombreOficial = producto.NombreComercial,
                NombreDetectado = sugerencia,
                ConfianzaFinal = 0.70,
                TipoSugerencia = "Recomendacion",
                RazonSugerencia = $"Sugerido por {itemIA.NombreDetectado}",
                ValidadoPorDigemid = false,
                StockDisponible = stock,
                TieneStock = stock > 0,
                Equivalentes = [],
            });
        }
    }

    private async Task<decimal> GetStockAsync(Guid medicamentoId, CancellationToken ct)
    {
        if (_sucursalId == null)
        {
            return 0;
        }

        return await dbContext.LotesEnSucursal
            .AsNoTracking()
            .Include(x => x.Lote)
            .Where(x => x.SucursalId == _sucursalId
                && x.Lote != null
                && x.Lote.ProductoId == medicamentoId
                && x.Lote.FechaCaducidad > DateTimeOffset.UtcNow)
            .SumAsync(x => x.CantidadFisica, ct);
    }

    private async Task<List<EquivalenteDto>> GetEquivalentesAsync(Guid medicamentoId, CancellationToken ct)
    {
        var formulaciones = await dbContext.FormulacionesClinicas
            .AsNoTracking()
            .Where(fc => fc.ProductoId == medicamentoId)
            .ToListAsync(ct);

        if (formulaciones.Count == 0)
        {
            return [];
        }

        var equivalentes = new List<EquivalenteDto>();

        foreach (var fc in formulaciones)
        {
            var otrosIds = await dbContext.FormulacionesClinicas
                .AsNoTracking()
                .Where(f => f.IngredienteId == fc.IngredienteId
                    && f.ConcentracionDosis == fc.ConcentracionDosis
                    && f.UnidadDosisClinica == fc.UnidadDosisClinica
                    && f.ProductoId != medicamentoId)
                .Select(f => f.ProductoId)
                .Distinct()
                .ToListAsync(ct);

            foreach (var otroId in otrosIds)
            {
                if (equivalentes.Exists(e => e.MedicamentoId == otroId))
                {
                    continue;
                }

                var med = await dbContext.Medicamentos
                    .AsNoTracking()
                    .Include(m => m.Laboratorio)
                    .FirstOrDefaultAsync(m => m.Id == otroId, ct);

                if (med == null)
                {
                    continue;
                }

                var stock = await GetStockAsync(otroId, ct);
                equivalentes.Add(new EquivalenteDto
                {
                    MedicamentoId = med.Id,
                    NombreComercial = med.NombreComercial,
                    Laboratorio = med.Laboratorio?.NombreCompania,
                    StockDisponible = stock,
                    TieneStock = stock > 0,
                });
            }
        }

        return equivalentes;
    }
}
