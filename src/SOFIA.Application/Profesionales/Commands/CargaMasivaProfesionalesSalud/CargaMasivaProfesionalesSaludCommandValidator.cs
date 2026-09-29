using FluentValidation;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Profesionales.Commands.CargaMasivaProfesionalesSalud;

public class CargaMasivaProfesionalesSaludCommandValidator : AbstractValidator<CargaMasivaProfesionalesSaludCommand>
{
    public CargaMasivaProfesionalesSaludCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.NumeroRegistro).NotEmpty().MaximumLength(50);
            _ = row.RuleFor(r => r.NombrePrescriptor).NotEmpty().MaximumLength(150);
            _ = row.RuleFor(r => r.DireccionClinica).MaximumLength(255);
        });
    }
}
