using FluentValidation;

namespace SOFIA.Application.PresentacionesVenta.Commands.CreatePresentacionVenta;

public class CreatePresentacionVentaCommandValidator : AbstractValidator<CreatePresentacionVentaCommand>
{
    public CreatePresentacionVentaCommandValidator()
    {
        _ = RuleFor(v => v.ProductoId).NotEmpty();
        _ = RuleFor(v => v.UnidadVentaId).NotEmpty();
        _ = RuleFor(v => v.PrecioVenta).GreaterThanOrEqualTo(0);
    }
}
