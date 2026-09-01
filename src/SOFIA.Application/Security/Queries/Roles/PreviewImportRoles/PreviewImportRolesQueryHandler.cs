using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Security.Queries.Roles.PreviewImportRoles;

public class PreviewImportRolesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportRolesQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportRolesQuery request, CancellationToken cancellationToken)
    {
        var existingNames = await context.Roles
            .Select(r => r.NombreRol.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingNames);

        var seenNames = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var nombre = row.Values.GetValueOrDefault("NombreRol");
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
                var nombre = row.Values.GetValueOrDefault("NombreRol");
                var nivelStr = row.Values.GetValueOrDefault("NivelJerarquia");

                if (string.IsNullOrWhiteSpace(nombre))
                {
                    errors.Add(new ValidationError("required", "nombreRol"));
                }
                else
                {
                    if (nombre.Length > 50) { errors.Add(new ValidationError("max-length", "nombreRol", new() { ["max"] = 50 })); } if (existingSet.Contains(nombre.ToLower())) { errors.Add(new ValidationError("duplicate", "nombreRol", new() { ["value"] = nombre })); } if (seenNames.TryGetValue(nombre, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "nombreRol", new() { ["value"] = nombre })); } }

                if (string.IsNullOrWhiteSpace(nivelStr)) { errors.Add(new ValidationError("required", "nivelJerarquia")); } else if (!int.TryParse(nivelStr, out var nivel)) { errors.Add(new ValidationError("invalid-integer", "nivelJerarquia")); } else if (nivel < 1) { errors.Add(new ValidationError("min-value", "nivelJerarquia", new() { ["min"] = 1 })); } return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
