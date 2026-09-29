using FluentValidation;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Proveedores.Commands.CargaMasivaProveedores;

public class CargaMasivaProveedoresCommandValidator : AbstractValidator<CargaMasivaProveedoresCommand>
{
    public CargaMasivaProveedoresCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.RazonSocial).NotEmpty().MaximumLength(200);
            _ = row.RuleFor(r => r.TaxId).NotEmpty().MaximumLength(50);
            _ = row.RuleFor(r => r.TerminosFinancieros).MaximumLength(100);
        });
    }
}
