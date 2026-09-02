using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Sucursales.Queries.PreviewImportSucursales;

public class PreviewImportSucursalesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportSucursalesQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportSucursalesQuery request, CancellationToken cancellationToken)
    {
        var existingLicencias = await context.Sucursales
            .Select(s => s.Numero_Licencia.ToLower())
            .ToListAsync(cancellationToken);
        var existingSet = new HashSet<string>(existingLicencias);

        var seenLicencias = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in request.Rows)
        {
            var licencia = row.Values.GetValueOrDefault("NumeroLicencia");
            if (!string.IsNullOrWhiteSpace(licencia))
            {
                if (!seenLicencias.ContainsKey(licencia))
                {
                    seenLicencias[licencia] = [];
                }

                seenLicencias[licencia].Add(row.RowNumber);
            }
        }

        return new PreviewResult
        {
            Rows = [.. request.Rows.Select(row =>
            {
                var errors = new List<ValidationError>();
                var nombre = row.Values.GetValueOrDefault("Nombre");
                var direccion = row.Values.GetValueOrDefault("DireccionFisica");
                var licencia = row.Values.GetValueOrDefault("NumeroLicencia");

                if (string.IsNullOrWhiteSpace(nombre)) { errors.Add(new ValidationError("required", "nombre")); } else if (nombre.Length > 100) { errors.Add(new ValidationError("max-length", "nombre", new() { ["max"] = 100 })); } if (string.IsNullOrWhiteSpace(direccion)) { errors.Add(new ValidationError("required", "direccionFisica")); } else if (direccion.Length > 255) { errors.Add(new ValidationError("max-length", "direccionFisica", new() { ["max"] = 255 })); } if (string.IsNullOrWhiteSpace(licencia))
                {
                    errors.Add(new ValidationError("required", "numeroLicencia"));
                }
                else
                {
                    if (licencia.Length > 50) { errors.Add(new ValidationError("max-length", "numeroLicencia", new() { ["max"] = 50 })); } if (existingSet.Contains(licencia.ToLower())) { errors.Add(new ValidationError("duplicate", "numeroLicencia", new() { ["value"] = licencia })); } if (seenLicencias.TryGetValue(licencia, out var rowsWithSame) && rowsWithSame.Count > 1) { errors.Add(new ValidationError("duplicate-in-file", "numeroLicencia", new() { ["value"] = licencia })); } }

                return new PreviewRowResult { RowNumber = row.RowNumber, Data = row.Values, Errors = errors };
            })]
        };
    }
}
