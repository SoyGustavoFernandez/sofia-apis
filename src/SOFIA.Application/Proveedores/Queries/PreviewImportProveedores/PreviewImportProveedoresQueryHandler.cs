using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Proveedores.Queries.PreviewImportProveedores;

public class PreviewImportProveedoresQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportProveedoresQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportProveedoresQuery request, CancellationToken cancellationToken)
    {
        var existingTaxIds = await context.Proveedores
            .Select(p => p.TaxId.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingTaxIds);

        var seenTaxIds = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var taxId = row.Values.GetValueOrDefault("TaxId");
            if (!string.IsNullOrWhiteSpace(taxId))
            {
                if (!seenTaxIds.ContainsKey(taxId))
                {
                    seenTaxIds[taxId] = [];
                }

                seenTaxIds[taxId].Add(row.RowNumber);
            }
        }

        return new PreviewResult
        {
            Rows = [.. request.Rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var razon = row.Values.GetValueOrDefault("RazonSocial");
                var taxId = row.Values.GetValueOrDefault("TaxId");
                var terminos = row.Values.GetValueOrDefault("TerminosFinancieros");
                var esgStr = row.Values.GetValueOrDefault("CalificacionEsg");
                var tasaStr = row.Values.GetValueOrDefault("TasaCumplimiento");

                if (string.IsNullOrWhiteSpace(razon)) { errors.Add(new ValidationError("required", "razonSocial")); } else if (razon.Length > 200) { errors.Add(new ValidationError("max-length", "razonSocial", new() { ["max"] = 200 })); } if (string.IsNullOrWhiteSpace(taxId))
                {
                    errors.Add(new ValidationError("required", "taxId"));
                }
                else
                {
                    if (taxId.Length > 50) { errors.Add(new ValidationError("max-length", "taxId", new() { ["max"] = 50 })); } if (existingSet.Contains(taxId.ToLower())) { errors.Add(new ValidationError("duplicate", "taxId", new() { ["value"] = taxId })); } if (seenTaxIds.TryGetValue(taxId, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "taxId", new() { ["value"] = taxId })); } }

                if (!string.IsNullOrEmpty(terminos) && terminos.Length > 100) { errors.Add(new ValidationError("max-length", "terminosFinancieros", new() { ["max"] = 100 })); } if (!string.IsNullOrEmpty(esgStr))
                {
                    if (!decimal.TryParse(esgStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var esg)) { errors.Add(new ValidationError("invalid-decimal", "calificacionEsg")); } else if (esg is < 0 or > 100) { errors.Add(new ValidationError("invalid-range", "calificacionEsg", new() { ["min"] = 0, ["max"] = 100 })); } }

                if (string.IsNullOrWhiteSpace(tasaStr)) { errors.Add(new ValidationError("required", "tasaCumplimiento")); } else if (!decimal.TryParse(tasaStr, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out var tasa)) { errors.Add(new ValidationError("invalid-decimal", "tasaCumplimiento")); } else if (tasa is < 0 or > 100) { errors.Add(new ValidationError("invalid-range", "tasaCumplimiento", new() { ["min"] = 0, ["max"] = 100 })); } return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
