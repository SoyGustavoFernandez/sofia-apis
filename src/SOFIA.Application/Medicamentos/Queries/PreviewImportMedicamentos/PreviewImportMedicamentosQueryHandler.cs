using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Queries.PreviewImportMedicamentos;

public class PreviewImportMedicamentosQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportMedicamentosQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportMedicamentosQuery request, CancellationToken cancellationToken)
    {
        var imports = new ImportContext(
            [.. await context.Medicamentos
                .Where(m => !m.IsDeleted)
                .Select(m => m.CodigoNacional.ToLower())
                .ToListAsync(cancellationToken)],
            [.. await context.Laboratorios
                .Select(l => l.NombreCompania.ToLower())
                .ToListAsync(cancellationToken)],
            [.. await context.UnidadesMedida
                .Select(u => u.Descripcion.ToLower())
                .ToListAsync(cancellationToken)],
            [.. Medicamento.CondicionesValidas.Select(c => c.ToLower())],
            BuildCodeOccurrences(request.Rows));

        return new PreviewResult
        {
            Rows = [.. request.Rows.Select(row => new PreviewRowResult
            {
                RowNumber = row.RowNumber,
                Data = row.Values,
                Errors = ValidateRow(row, imports),
            })],
        };
    }

    private static Dictionary<string, int> BuildCodeOccurrences(List<ExcelRow> rows)
    {
        var occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var row in rows)
        {
            var codigo = row.Values.GetValueOrDefault("CodigoNacional");
            if (!string.IsNullOrWhiteSpace(codigo))
            {
                occurrences[codigo] = occurrences.GetValueOrDefault(codigo) + 1;
            }
        }

        return occurrences;
    }

    private static List<ValidationError> ValidateRow(ExcelRow row, ImportContext ctx)
    {
        var errors = new List<ValidationError>();
        ValidateCodigoNacional(row.Values.GetValueOrDefault("CodigoNacional"), ctx, errors);
        ValidateText(
            row.Values.GetValueOrDefault("NombreComercial"),
            "nombreComercial",
            Medicamento.NombreComercialMaxLength,
            errors);
        ValidateReference(row.Values.GetValueOrDefault("Laboratorio"), "laboratorio", ctx.Laboratorios, errors);
        ValidateReference(row.Values.GetValueOrDefault("UnidadBase"), "unidadBase", ctx.Unidades, errors);
        ValidateCondicionVenta(row.Values.GetValueOrDefault("CondicionVenta"), ctx, errors);
        return errors;
    }

    private static void ValidateCodigoNacional(string? codigo, ImportContext ctx, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(codigo))
        {
            errors.Add(new ValidationError("required", "codigoNacional"));
            return;
        }

        if (codigo.Length > Medicamento.CodigoNacionalMaxLength)
        {
            errors.Add(new ValidationError("max-length", "codigoNacional", new() { ["max"] = Medicamento.CodigoNacionalMaxLength }));
        }

        if (ctx.ExistingCodes.Contains(codigo.ToLower()))
        {
            errors.Add(new ValidationError("duplicate", "codigoNacional", new() { ["value"] = codigo }));
        }

        if (ctx.CodeOccurrences.GetValueOrDefault(codigo) > 1)
        {
            errors.Add(new ValidationError("duplicate-in-file", "codigoNacional", new() { ["value"] = codigo }));
        }
    }

    private static void ValidateText(string? value, string field, int maxLength, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError("required", field));
        }
        else if (value.Length > maxLength)
        {
            errors.Add(new ValidationError("max-length", field, new() { ["max"] = maxLength }));
        }
    }

    private static void ValidateReference(string? value, string field, HashSet<string> catalog, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError("required", field));
        }
        else if (!catalog.Contains(value.ToLower()))
        {
            errors.Add(new ValidationError("not-found", field, new() { ["value"] = value }));
        }
    }

    private static void ValidateCondicionVenta(string? value, ImportContext ctx, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError("required", "condicionVenta"));
        }
        else if (!ctx.Condiciones.Contains(value.ToLower()))
        {
            errors.Add(new ValidationError("invalid-option", "condicionVenta", new() { ["value"] = value }));
        }
    }

    private sealed record ImportContext(
        HashSet<string> ExistingCodes,
        HashSet<string> Laboratorios,
        HashSet<string> Unidades,
        HashSet<string> Condiciones,
        Dictionary<string, int> CodeOccurrences);
}
