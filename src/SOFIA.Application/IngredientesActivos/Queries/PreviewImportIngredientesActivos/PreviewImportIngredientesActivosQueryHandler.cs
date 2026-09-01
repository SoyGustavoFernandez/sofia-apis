using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.IngredientesActivos.Queries.PreviewImportIngredientesActivos;

public class PreviewImportIngredientesActivosQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportIngredientesActivosQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportIngredientesActivosQuery request, CancellationToken cancellationToken)
    {
        var existingCodes = await context.IngredientesActivos
            .Select(i => i.CodigoAtc.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingCodes);

        var seenCodes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var code = row.Values.GetValueOrDefault("CodigoAtc");
            if (!string.IsNullOrWhiteSpace(code))
            {
                if (!seenCodes.ContainsKey(code))
                {
                    seenCodes[code] = [];
                }

                seenCodes[code].Add(row.RowNumber);
            }
        }

        return new PreviewResult
        {
            Rows = [.. request.Rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var dci = row.Values.GetValueOrDefault("DenominacionDci");
                var atc = row.Values.GetValueOrDefault("CodigoAtc");

                if (string.IsNullOrWhiteSpace(dci)) { errors.Add(new ValidationError("required", "denominacionDci")); } else if (dci.Length > 255) { errors.Add(new ValidationError("max-length", "denominacionDci", new() { ["max"] = 255 })); } if (string.IsNullOrWhiteSpace(atc))
                {
                    errors.Add(new ValidationError("required", "codigoAtc"));
                }
                else
                {
                    if (atc.Length > 15) { errors.Add(new ValidationError("max-length", "codigoAtc", new() { ["max"] = 15 })); } if (existingSet.Contains(atc.ToLower())) { errors.Add(new ValidationError("duplicate", "codigoAtc", new() { ["value"] = atc })); } if (seenCodes.TryGetValue(atc, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "codigoAtc", new() { ["value"] = atc })); } }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
