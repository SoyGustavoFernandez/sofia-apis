using FluentValidation;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Security.Commands.Roles.CargaMasivaRoles;

public class CargaMasivaRolesCommandValidator : AbstractValidator<CargaMasivaRolesCommand>
{
    public CargaMasivaRolesCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.NombreRol).NotEmpty().MaximumLength(50);
            _ = row.RuleFor(r => r.Descripcion).MaximumLength(255);
            _ = row.RuleFor(r => r.NivelJerarquia).GreaterThanOrEqualTo(0);
        });
    }
}
