using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.DIGEMID.Queries.PreviewImportDigemid;

public class PreviewImportDigemidQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportDigemidQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportDigemidQuery request, CancellationToken cancellationToken)
    {
        var existingCodes = await context.DigemidCatalogoProductos
            .Select(d => d.CodProd.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingCodes);

        var seenCodes = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var cod = row.Values.GetValueOrDefault("CodProd");
            if (!string.IsNullOrWhiteSpace(cod))
            {
                if (!seenCodes.ContainsKey(cod))
                {
                    seenCodes[cod] = [];
                }

                seenCodes[cod].Add(row.RowNumber);
            }
        }

        return new PreviewResult
        {
            Rows = [.. request.Rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var cod = row.Values.GetValueOrDefault("CodProd");
                var nom = row.Values.GetValueOrDefault("NomProd");
                var estado = row.Values.GetValueOrDefault("Estado");

                if (string.IsNullOrWhiteSpace(cod))
                {
                    errors.Add(new ValidationError("required", "codProd"));
                }
                else
                {
                    if (existingSet.Contains(cod.ToLower())) { errors.Add(new ValidationError("duplicate", "codProd", new() { ["value"] = cod })); } if (seenCodes.TryGetValue(cod, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "codProd", new() { ["value"] = cod })); } }

                if (string.IsNullOrWhiteSpace(nom)) { errors.Add(new ValidationError("required", "nomProd")); } if (string.IsNullOrWhiteSpace(estado)) { errors.Add(new ValidationError("required", "estado")); } return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
