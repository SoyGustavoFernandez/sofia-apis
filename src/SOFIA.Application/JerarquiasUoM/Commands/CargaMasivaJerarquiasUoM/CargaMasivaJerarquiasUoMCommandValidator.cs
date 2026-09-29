using FluentValidation;
using SOFIA.Application.Common.Excel;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.JerarquiasUoM.Commands.CargaMasivaJerarquiasUoM;

public class CargaMasivaJerarquiasUoMCommandValidator : AbstractValidator<CargaMasivaJerarquiasUoMCommand>
{
    public CargaMasivaJerarquiasUoMCommandValidator()
    {
        _ = RuleFor(v => v.Rows).WithinImportLimits();
        _ = RuleForEach(v => v.Rows).ChildRules(row =>
        {
            _ = row.RuleFor(r => r.Producto).NotEmpty().MaximumLength(Medicamento.NombreComercialMaxLength);
            _ = row.RuleFor(r => r.UnidadMayor).NotEmpty().MaximumLength(50);
            _ = row.RuleFor(r => r.UnidadMenor).NotEmpty().MaximumLength(50);
            _ = row.RuleFor(r => r.Multiplicador).NotEmpty().MaximumLength(30);
        });
    }
}
