using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;
using SOFIA.Infrastructure.Utils;

namespace SOFIA.Infrastructure.Services;

public class BuscadorFuzzyService(IApplicationDbContext context) : IBuscadorService
{
    private readonly IApplicationDbContext _context = context;

    public async Task<List<DigemidCatalogoProducto>> BuscarEnDigemidAsync(string termino, int top = 5, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(termino))
        {
            return [];
        }

        var normalizedTermino = termino.ToUpper();

        // 1. First, get all items that contain some words from the term to reduce memory load
        var terms = termino.Split([' '], StringSplitOptions.RemoveEmptyEntries)
                           .Select(t => t.ToUpper())
                           .ToList();

        var query = _context.DigemidCatalogoProductos.AsQueryable();

        foreach (var t in terms)
        {
            // Try to match anything containing at least one part of the term
            query = query.Where(x => x.NomProd.ToUpper().Contains(t));
        }

        var candidatos = await query.Take(50).ToListAsync(cancellationToken);

        // Fallback: If no results with AND logic, use OR logic or just get top 200
        if (!candidatos.Any())
        {
            candidatos = await _context.DigemidCatalogoProductos
                .Where(x => x.NomProd.ToUpper().Contains(terms[0]))
                .Take(50)
                .ToListAsync(cancellationToken);
        }

        // 2. Perform Levenshtein distance locally (memory)
        var mejores = candidatos
            .OrderBy(c => StringUtils.LevenshteinDistance(normalizedTermino, c.NomProd.ToUpper()))
            .Take(top)
            .ToList();

        return mejores;
    }

    public async Task<Medicamento?> BuscarMejorCoincidenciaAsync(string termino, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(termino))
        {
            return null;
        }

        var normalizedTermino = termino.ToUpper();

        // 1. Get potential candidates from DB
        var terms = termino.Split([' '], StringSplitOptions.RemoveEmptyEntries)
                           .Select(t => t.ToUpper())
                           .ToList();

        var query = _context.Medicamentos.AsQueryable();

        if (terms.Any())
        {
            query = query.Where(x => x.NombreComercial.ToUpper().Contains(terms[0]));
        }

        var candidatos = await query.Take(50).ToListAsync(cancellationToken);

        if (!candidatos.Any())
        {
            return null;
        }

        // 2. Calculate Levenshtein in memory and find the best match
        var mejorMatch = candidatos
            .OrderBy(c => StringUtils.LevenshteinDistance(normalizedTermino, c.NombreComercial.ToUpper()))
            .FirstOrDefault();

        // Only return if similarity is acceptable (e.g., > 40%)
        if (mejorMatch != null)
        {
            var similarity = StringUtils.CalculateSimilarity(normalizedTermino, mejorMatch.NombreComercial.ToUpper());
            if (similarity > 0.4)
            {
                return mejorMatch;
            }
        }

        return null;
    }
}
