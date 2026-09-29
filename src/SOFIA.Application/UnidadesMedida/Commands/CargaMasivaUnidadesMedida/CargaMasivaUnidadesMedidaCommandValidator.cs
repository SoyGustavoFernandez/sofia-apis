using FluentValidation;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.UnidadesMedida.Commands.CargaMasivaUnidadesMedida;

public class CargaMasivaUnidadesMedidaCommandValidator : AbstractValidator<CargaMasivaUnidadesMedidaCommand>
{
    public CargaMasivaUnidadesMedidaCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.Codigo).NotEmpty().MaximumLength(10);
            _ = row.RuleFor(r => r.Descripcion).NotEmpty().MaximumLength(50);
        });
    }
}
