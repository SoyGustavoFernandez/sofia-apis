using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Laboratorios.Queries.PreviewImportLaboratorios;

public class PreviewImportLaboratoriosQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportLaboratoriosQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportLaboratoriosQuery request, CancellationToken cancellationToken)
    {
        var existingNames = await context.Laboratorios
            .Select(l => l.NombreCompania.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingNames);

        var seenNames = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var nombre = row.Values.GetValueOrDefault("NombreCompania");
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
                var nombre = row.Values.GetValueOrDefault("NombreCompania");
                var codigo = row.Values.GetValueOrDefault("CodigoIdentificador");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new ValidationError("required", "nombreCompania"));
                }
                else
                {
                    if (nombre.Length > 150) { errors.Add(new ValidationError("max-length", "nombreCompania", new() { ["max"] = 150 })); } if (existingSet.Contains(nombre.ToLower())) { errors.Add(new ValidationError("duplicate", "nombreCompania", new() { ["value"] = nombre })); } if (seenNames.TryGetValue(nombre, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "nombreCompania", new() { ["value"] = nombre })); } }

                if (!string.IsNullOrEmpty(codigo) && codigo.Length > 50) { errors.Add(new ValidationError("max-length", "codigoIdentificador", new() { ["max"] = 50 })); } return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
