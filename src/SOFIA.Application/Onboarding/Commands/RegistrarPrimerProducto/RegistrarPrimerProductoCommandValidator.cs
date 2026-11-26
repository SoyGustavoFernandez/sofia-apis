using FluentValidation;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Onboarding.Commands.RegistrarPrimerProducto;

public class RegistrarPrimerProductoCommandValidator : AbstractValidator<RegistrarPrimerProductoCommand>
{
    public RegistrarPrimerProductoCommandValidator()
    {
        _ = RuleFor(v => v.CodigoNacional)
            .NotEmpty().WithMessage("National code is required.")
            .MaximumLength(Medicamento.CodigoNacionalMaxLength);

        _ = RuleFor(v => v.NombreComercial)
            .NotEmpty().WithMessage("Commercial name is required.")
            .MaximumLength(Medicamento.NombreComercialMaxLength);

        _ = RuleFor(v => v.LaboratorioNombre)
            .NotEmpty().WithMessage("Laboratory is required.")
            .MaximumLength(150);

        _ = RuleFor(v => v.UnidadBaseId)
            .NotEmpty().WithMessage("Unit of measure is required.");

        _ = RuleFor(v => v.CondicionVenta)
            .IsInEnum().WithMessage("Invalid sale condition.");

        _ = RuleFor(v => v.PrecioVentaBase)
            .GreaterThan(0).WithMessage("Sale price must be greater than zero.");

        _ = RuleFor(v => v.NumeroLote)
            .NotEmpty().WithMessage("Batch number is required.")
            .MaximumLength(100);

        _ = RuleFor(v => v.FechaCaducidad)
            .NotEmpty().WithMessage("Expiration date is required.")
            .Must(fecha => !LoteInventario.CaducidadVencida(fecha, DateTimeOffset.UtcNow)).WithMessage("Expiration date cannot be in the past.");

        _ = RuleFor(v => v.Cantidad)
            .GreaterThan(0).WithMessage("Quantity must be greater than zero.");
    }
}
