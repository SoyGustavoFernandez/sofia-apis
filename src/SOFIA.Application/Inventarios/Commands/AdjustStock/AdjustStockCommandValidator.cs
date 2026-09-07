using FluentValidation;

namespace SOFIA.Application.Inventarios.Commands.AdjustStock;

public class AdjustStockCommandValidator : AbstractValidator<AdjustStockCommand>
{
    public AdjustStockCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.NuevaCantidad)
            .GreaterThanOrEqualTo(0).WithMessage("Quantity cannot be negative.");
    }
}
