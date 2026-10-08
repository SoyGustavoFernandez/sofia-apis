using FluentValidation;

namespace SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;

public class RegistrarPrecioProveedorCommandValidator : AbstractValidator<RegistrarPrecioProveedorCommand>
{
    // Stays well inside the DECIMAL(12,4) column of the cost
    public const decimal PrecioCompraMaximo = 9_999_999.9999m;

    public RegistrarPrecioProveedorCommandValidator()
    {
        _ = RuleFor(v => v.ProveedorId).NotEmpty();
        _ = RuleFor(v => v.MedicamentoId).NotEmpty();
        _ = RuleFor(v => v.PrecioCompra).GreaterThan(0).LessThanOrEqualTo(PrecioCompraMaximo);
        _ = RuleFor(v => v.DescuentoPorcentaje).InclusiveBetween(0, 100);
    }
}
