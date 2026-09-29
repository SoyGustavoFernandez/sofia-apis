using FluentValidation;
using SOFIA.Application.Common.Excel;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Medicamentos.Commands.CargaMasivaMedicamentos;

public class CargaMasivaMedicamentosCommandValidator : AbstractValidator<CargaMasivaMedicamentosCommand>
{
    public CargaMasivaMedicamentosCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.CodigoNacional).NotEmpty().MaximumLength(Medicamento.CodigoNacionalMaxLength);
            _ = row.RuleFor(r => r.NombreComercial).NotEmpty().MaximumLength(Medicamento.NombreComercialMaxLength);
            _ = row.RuleFor(r => r.Laboratorio).NotEmpty().MaximumLength(150);
            _ = row.RuleFor(r => r.UnidadBase).NotEmpty().MaximumLength(50);
            _ = row.RuleFor(r => r.CondicionVenta).NotEmpty().MaximumLength(50);
        });
    }
}
