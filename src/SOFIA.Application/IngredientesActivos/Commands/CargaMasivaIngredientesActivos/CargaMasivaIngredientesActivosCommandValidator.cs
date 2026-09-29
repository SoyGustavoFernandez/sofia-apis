using FluentValidation;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.IngredientesActivos.Commands.CargaMasivaIngredientesActivos;

public class CargaMasivaIngredientesActivosCommandValidator : AbstractValidator<CargaMasivaIngredientesActivosCommand>
{
    public CargaMasivaIngredientesActivosCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.DenominacionDci).NotEmpty().MaximumLength(255);
            _ = row.RuleFor(r => r.CodigoAtc).NotEmpty().MaximumLength(15);
        });
    }
}
