using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Seguros.Queries.PreviewImportSeguros;

public class PreviewImportSegurosQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportSegurosQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportSegurosQuery request, CancellationToken cancellationToken)
    {
        var existingCodes = await context.Aseguradoras
            .Select(s => s.CodigoIdentificadorNacional.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingCodes);

        var seenCodes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var code = row.Values.GetValueOrDefault("CodigoIdentificadorNacional");
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
                var nombre = row.Values.GetValueOrDefault("NombreComercial");
                var codigo = row.Values.GetValueOrDefault("CodigoIdentificadorNacional");

                if (string.IsNullOrWhiteSpace(nombre)) { errors.Add(new ValidationError("required", "nombreComercial")); } else if (nombre.Length > 150) { errors.Add(new ValidationError("max-length", "nombreComercial", new() { ["max"] = 200 })); } if (string.IsNullOrWhiteSpace(codigo))
                {
                    errors.Add(new ValidationError("required", "codigoIdentificadorNacional"));
                }
                else
                {
                    if (codigo.Length > 50) { errors.Add(new ValidationError("max-length", "codigoIdentificadorNacional", new() { ["max"] = 50 })); } if (existingSet.Contains(codigo.ToLower())) { errors.Add(new ValidationError("duplicate", "codigoIdentificadorNacional", new() { ["value"] = codigo })); } if (seenCodes.TryGetValue(codigo, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "codigoIdentificadorNacional", new() { ["value"] = codigo })); } }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
