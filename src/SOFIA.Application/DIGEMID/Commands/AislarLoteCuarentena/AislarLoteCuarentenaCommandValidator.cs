using FluentValidation;

namespace SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;

public class AislarLoteCuarentenaCommandValidator : AbstractValidator<AislarLoteCuarentenaCommand>
{
    public AislarLoteCuarentenaCommandValidator()
    {
        _ = RuleFor(x => x.LoteId).NotEmpty();
        _ = RuleFor(x => x.CantidadAislada).GreaterThan(0);
        _ = RuleFor(x => x.MotivoAislamiento).NotEmpty().MaximumLength(50);
        _ = RuleFor(x => x.EstadoResolucion).NotEmpty().MaximumLength(20);
    }
}
