using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Pacientes.Queries.PreviewImportPacientes;

public class PreviewImportPacientesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportPacientesQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportPacientesQuery request, CancellationToken cancellationToken)
    {
        var existingDocs = await context.Pacientes
            .Select(p => p.DocIdentidadGub.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingDocs);

        var seenDocs = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var doc = row.Values.GetValueOrDefault("DocIdentidadGub");
            if (!string.IsNullOrWhiteSpace(doc))
            {
                if (!seenDocs.ContainsKey(doc))
                {
                    seenDocs[doc] = [];
                }

                seenDocs[doc].Add(row.RowNumber);
            }
        }

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        return new PreviewResult
        {
            Rows = [.. request.Rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var doc = row.Values.GetValueOrDefault("DocIdentidadGub");
                var nombre = row.Values.GetValueOrDefault("NombreApellidos");
                var fechaStr = row.Values.GetValueOrDefault("FechaNacimiento");
                var contacto = row.Values.GetValueOrDefault("ContactoPrimario");

                if (string.IsNullOrWhiteSpace(doc))
                {
                    errors.Add(new ValidationError("required", "docIdentidadGub"));
                }
                else
                {
                    if (doc.Length > 50) { errors.Add(new ValidationError("max-length", "docIdentidadGub", new() { ["max"] = 50 })); } if (existingSet.Contains(doc.ToLower())) { errors.Add(new ValidationError("duplicate", "docIdentidadGub", new() { ["value"] = doc })); } if (seenDocs.TryGetValue(doc, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "docIdentidadGub", new() { ["value"] = doc })); } }

                if (string.IsNullOrWhiteSpace(nombre)) { errors.Add(new ValidationError("required", "nombreApellidos")); } else if (nombre.Length > 200) { errors.Add(new ValidationError("max-length", "nombreApellidos", new() { ["max"] = 200 })); } if (string.IsNullOrWhiteSpace(fechaStr)) { errors.Add(new ValidationError("required", "fechaNacimiento")); } else if (!DateOnly.TryParseExact(fechaStr, "dd/MM/yyyy", out var fecha)) { errors.Add(new ValidationError("invalid-date", "fechaNacimiento")); } else if (fecha > today) { errors.Add(new ValidationError("future-date", "fechaNacimiento")); } if (!string.IsNullOrEmpty(contacto) && contacto.Length > 100) { errors.Add(new ValidationError("max-length", "contactoPrimario", new() { ["max"] = 100 })); } return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
