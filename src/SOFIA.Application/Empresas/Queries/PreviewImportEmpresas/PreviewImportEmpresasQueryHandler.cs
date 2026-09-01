using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Empresas.Queries.PreviewImportEmpresas;

public class PreviewImportEmpresasQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportEmpresasQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportEmpresasQuery request, CancellationToken cancellationToken)
    {
        var existingNames = await context.Empresas
            .Select(e => e.Nombre.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingNames);

        var seenNames = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var nombre = row.Values.GetValueOrDefault("Nombre");
            if (!string.IsNullOrWhiteSpace(nombre))
            {
                if (!seenNames.ContainsKey(nombre))
                {
                    seenNames[nombre] = [];
                }

                seenNames[nombre].Add(row.RowNumber);
            }
        }

        return new PreviewResult
        {
            Rows = [.. request.Rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var nombre = row.Values.GetValueOrDefault("Nombre");
                var ruc = row.Values.GetValueOrDefault("RUC");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new ValidationError("required", "nombre"));
                }
                else
                {
                    if (existingSet.Contains(nombre.ToLower())) { errors.Add(new ValidationError("duplicate", "nombre", new() { ["value"] = nombre })); } if (seenNames.TryGetValue(nombre, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "nombre", new() { ["value"] = nombre })); } }

                if (!string.IsNullOrEmpty(ruc) && (ruc.Length != 11 || !ruc.All(char.IsDigit))) { errors.Add(new ValidationError("invalid-digits", "ruc", new() { ["digits"] = 11 })); } return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
