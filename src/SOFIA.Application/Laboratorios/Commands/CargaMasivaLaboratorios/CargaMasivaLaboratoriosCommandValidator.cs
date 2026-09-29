using FluentValidation;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Laboratorios.Commands.CargaMasivaLaboratorios;

public class CargaMasivaLaboratoriosCommandValidator : AbstractValidator<CargaMasivaLaboratoriosCommand>
{
    public CargaMasivaLaboratoriosCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.NombreCompania).NotEmpty().MaximumLength(150);
            _ = row.RuleFor(r => r.CodigoIdentificador).MaximumLength(50);
        });
    }
}
