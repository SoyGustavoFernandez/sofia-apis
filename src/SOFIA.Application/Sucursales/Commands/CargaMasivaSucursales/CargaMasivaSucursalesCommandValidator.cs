using FluentValidation;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Sucursales.Commands.CargaMasivaSucursales;

public class CargaMasivaSucursalesCommandValidator : AbstractValidator<CargaMasivaSucursalesCommand>
{
    public CargaMasivaSucursalesCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.Nombre).NotEmpty().MaximumLength(100);
            _ = row.RuleFor(r => r.DireccionFisica).NotEmpty().MaximumLength(255);
            _ = row.RuleFor(r => r.NumeroLicencia).NotEmpty().MaximumLength(50);
        });
    }
}
