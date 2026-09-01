using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Profesionales.Queries.PreviewImportProfesionalesSalud;

public class PreviewImportProfesionalesSaludQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportProfesionalesSaludQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportProfesionalesSaludQuery request, CancellationToken cancellationToken)
    {
        var existingRegistros = await context.ProfesionalesSalud
            .Select(p => p.NumeroRegistro.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingRegistros);

        var seenRegistros = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var reg = row.Values.GetValueOrDefault("NumeroRegistro");
            if (!string.IsNullOrWhiteSpace(reg))
            {
                if (!seenRegistros.ContainsKey(reg))
                {
                    seenRegistros[reg] = [];
                }

                seenRegistros[reg].Add(row.RowNumber);
            }
        }

        return new PreviewResult
        {
            Rows = [.. request.Rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var reg = row.Values.GetValueOrDefault("NumeroRegistro");
                var nombre = row.Values.GetValueOrDefault("NombrePrescriptor");
                var direccion = row.Values.GetValueOrDefault("DireccionClinica");

                if (string.IsNullOrWhiteSpace(reg))
                {
                    errors.Add(new ValidationError("required", "numeroRegistro"));
                }
                else
                {
                    if (reg.Length > 50) { errors.Add(new ValidationError("max-length", "numeroRegistro", new() { ["max"] = 50 })); } if (existingSet.Contains(reg.ToLower())) { errors.Add(new ValidationError("duplicate", "numeroRegistro", new() { ["value"] = reg })); } if (seenRegistros.TryGetValue(reg, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "numeroRegistro", new() { ["value"] = reg })); } }

                if (string.IsNullOrWhiteSpace(nombre)) { errors.Add(new ValidationError("required", "nombrePrescriptor")); } else if (nombre.Length > 150) { errors.Add(new ValidationError("max-length", "nombrePrescriptor", new() { ["max"] = 150 })); } if (!string.IsNullOrEmpty(direccion) && direccion.Length > 255) { errors.Add(new ValidationError("max-length", "direccionClinica", new() { ["max"] = 255 })); } return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
