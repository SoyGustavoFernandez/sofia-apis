using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.UnidadesMedida.Queries.PreviewImportUnidadesMedida;

public class PreviewImportUnidadesMedidaQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportUnidadesMedidaQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportUnidadesMedidaQuery request, CancellationToken cancellationToken)
    {
        var existingCodes = await context.UnidadesMedida
            .Select(u => u.Codigo.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingCodes);

        var seenCodes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var code = row.Values.GetValueOrDefault("Codigo");
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
                var codigo = row.Values.GetValueOrDefault("Codigo");
                var descripcion = row.Values.GetValueOrDefault("Descripcion");

                if (string.IsNullOrWhiteSpace(codigo))
                {
                    errors.Add(new ValidationError("required", "codigo"));
                }
                else
                {
                    if (codigo.Length > 10) { errors.Add(new ValidationError("max-length", "codigo", new() { ["max"] = 10 })); } if (existingSet.Contains(codigo.ToLower())) { errors.Add(new ValidationError("duplicate", "codigo", new() { ["value"] = codigo })); } if (seenCodes.TryGetValue(codigo, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "codigo", new() { ["value"] = codigo })); } }

                if (string.IsNullOrWhiteSpace(descripcion)) { errors.Add(new ValidationError("required", "descripcion")); } else if (descripcion.Length > 50) { errors.Add(new ValidationError("max-length", "descripcion", new() { ["max"] = 50 })); } return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
