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
        var productos = new HashSet<string>(await context.Medicamentos
            .Where(m => !m.IsDeleted)
            .Select(m => m.NombreComercial.ToLower())
            .ToListAsync(cancellationToken));
        var unidades = new HashSet<string>(await context.UnidadesMedida
            .Select(u => u.Descripcion.ToLower())
            .ToListAsync(cancellationToken));

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

    private static List<ValidationError> ValidateRow(ExcelRow row, HashSet<string> productos, HashSet<string> unidades)
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
