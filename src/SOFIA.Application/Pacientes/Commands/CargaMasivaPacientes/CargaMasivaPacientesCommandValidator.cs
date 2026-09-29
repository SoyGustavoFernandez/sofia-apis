using FluentValidation;
using SOFIA.Application.Common.Excel;

namespace SOFIA.Application.Pacientes.Commands.CargaMasivaPacientes;

public class CargaMasivaPacientesCommandValidator : AbstractValidator<CargaMasivaPacientesCommand>
{
    public CargaMasivaPacientesCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.DocIdentidadGub).NotEmpty().MaximumLength(50);
            _ = row.RuleFor(r => r.NombreApellidos).NotEmpty().MaximumLength(200);
            _ = row.RuleFor(r => r.FechaNacimiento).NotEmpty().MaximumLength(10);
            _ = row.RuleFor(r => r.ContactoPrimario).MaximumLength(100);
        });
    }
}
