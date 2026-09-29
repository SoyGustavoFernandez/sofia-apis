using FluentValidation;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.DIGEMID.Commands.CargaMasivaDigemid;

public class CargaMasivaDigemidCommandValidator : AbstractValidator<CargaMasivaDigemidCommand>
{
    public CargaMasivaDigemidCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.CodProd).NotEmpty().MaximumLength(20);
            _ = row.RuleFor(r => r.NomProd).NotEmpty().MaximumLength(255);
            _ = row.RuleFor(r => r.Concent).MaximumLength(255);
            _ = row.RuleFor(r => r.FormaFarmaceutica).MaximumLength(150);
            _ = row.RuleFor(r => r.Fraccion).MaximumLength(100);
            _ = row.RuleFor(r => r.RegistroSanitario).MaximumLength(50);
            _ = row.RuleFor(r => r.Titular).MaximumLength(255);
            _ = row.RuleFor(r => r.Estado).NotEmpty().MaximumLength(50);
        });
    }
}
