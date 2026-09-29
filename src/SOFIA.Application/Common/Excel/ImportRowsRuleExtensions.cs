using FluentValidation;

namespace SOFIA.Application.Common.Excel;

public static class ImportRowsRuleExtensions
{
    // Shared by every CargaMasiva*CommandValidator so the row cap matches the one enforced when reading the .xlsx
    public static IRuleBuilderOptions<T, List<TRow>> WithinImportLimits<T, TRow>(this IRuleBuilder<T, List<TRow>> rule) =>
        rule.NotEmpty()
            .Must(rows => rows is null || rows.Count <= ImportLimits.MaxRows)
            .WithMessage($"A bulk load cannot exceed {ImportLimits.MaxRows} rows.");
}
