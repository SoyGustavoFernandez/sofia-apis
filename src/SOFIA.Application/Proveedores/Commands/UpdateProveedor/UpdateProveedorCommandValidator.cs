using FluentValidation;
using SOFIA.Domain.ValueObjects;

namespace SOFIA.Application.Proveedores.Commands.UpdateProveedor;

public class UpdateProveedorCommandValidator : AbstractValidator<UpdateProveedorCommand>
{
    public UpdateProveedorCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("Supplier is required.");

        _ = RuleFor(v => v.RazonSocial)
            .NotEmpty().WithMessage("Business name is required.")
            .MaximumLength(200).WithMessage("Business name must not exceed 200 characters.");

        _ = RuleFor(v => v.TaxId)
            .Must(taxId => Ruc.Create(taxId).IsSuccess).WithMessage("Tax ID must be an 11-digit RUC.");

        _ = RuleFor(v => v.TerminosFinancieros)
            .MaximumLength(100).WithMessage("Financial terms must not exceed 100 characters.");

        _ = RuleFor(v => v.CalificacionEsg)
            .InclusiveBetween(0m, 100m).WithMessage("ESG rating must be between 0 and 100.");

        _ = RuleFor(v => v.TasaCumplimiento)
            .InclusiveBetween(0m, 100m).WithMessage("Compliance rate must be between 0 and 100.");
    }
}
