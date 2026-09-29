using System.Globalization;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Excel;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.JerarquiasUoM.Queries.PreviewImportJerarquiasUoM;

public class PreviewImportJerarquiasUoMQueryHandler(IApplicationDbContext context)
    : IRequestHandler<PreviewImportJerarquiasUoMQuery, PreviewResult>
{
    public async Task<PreviewResult> Handle(PreviewImportJerarquiasUoMQuery request, CancellationToken cancellationToken)
    {
        var productos = new ImportNameLookup((await context.Medicamentos
            .Where(m => !m.IsDeleted)
            .Select(m => new { m.NombreComercial, m.Id })
            .ToListAsync(cancellationToken)).Select(m => (m.NombreComercial, m.Id)));
        var unidades = new ImportNameLookup((await context.UnidadesMedida
            .Select(u => new { u.Descripcion, u.Id })
            .ToListAsync(cancellationToken)).Select(u => (u.Descripcion, u.Id)));

        return new PreviewResult
        {
            Rows = [.. request.Rows.Select(row => new PreviewRowResult
            {
                RowNumber = row.RowNumber,
                Data = row.Values,
                Errors = ValidateRow(row, productos, unidades),
            })],
        };
    }

    private static List<ValidationError> ValidateRow(ExcelRow row, ImportNameLookup productos, ImportNameLookup unidades)
    {
        var errors = new List<ValidationError>();
        var producto = row.Values.GetValueOrDefault("Producto");
        var unidadMayor = row.Values.GetValueOrDefault("UnidadMayor");
        var unidadMenor = row.Values.GetValueOrDefault("UnidadMenor");
        var multiplicador = row.Values.GetValueOrDefault("Multiplicador");

        ValidateReference(producto, "producto", productos, errors);
        ValidateReference(unidadMayor, "unidadMayor", unidades, errors);
        ValidateReference(unidadMenor, "unidadMenor", unidades, errors);

        if (!string.IsNullOrWhiteSpace(unidadMayor)
            && string.Equals(unidadMayor, unidadMenor, StringComparison.OrdinalIgnoreCase))
        {
            errors.Add(new ValidationError("must-differ", "unidadMenor", new() { ["other"] = "unidadMayor" }));
        }

        ValidateMultiplicador(multiplicador, errors);
        return errors;
    }

    private static void ValidateReference(string? value, string field, ImportNameLookup catalog, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError("required", field));
        }
        else if (!catalog.Contains(value))
        {
            errors.Add(new ValidationError("not-found", field, new() { ["value"] = value }));
        }
        else if (catalog.IsAmbiguous(value))
        {
            errors.Add(new ValidationError("ambiguous", field, new() { ["value"] = value }));
        }
    }

    private static void ValidateMultiplicador(string? value, List<ValidationError> errors)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            errors.Add(new ValidationError("required", "multiplicador"));
        }
        else if (!decimal.TryParse(value, NumberStyles.Number, CultureInfo.InvariantCulture, out var parsed))
        {
            errors.Add(new ValidationError("invalid-decimal", "multiplicador"));
        }
        else if (parsed <= 0)
        {
            errors.Add(new ValidationError("positive-number", "multiplicador"));
        }
    }
}
