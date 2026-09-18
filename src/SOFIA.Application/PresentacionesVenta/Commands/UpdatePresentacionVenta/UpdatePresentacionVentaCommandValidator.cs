using FluentValidation;

namespace SOFIA.Application.PresentacionesVenta.Commands.UpdatePresentacionVenta;

public class UpdatePresentacionVentaCommandValidator : AbstractValidator<UpdatePresentacionVentaCommand>
{
    public UpdatePresentacionVentaCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty();
        _ = RuleFor(v => v.UnidadVentaId).NotEmpty();
        _ = RuleFor(v => v.PrecioVenta).GreaterThanOrEqualTo(0);
    }
}
