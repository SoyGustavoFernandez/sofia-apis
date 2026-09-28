using FluentValidation;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;

public class AislarLoteCuarentenaCommandValidator : AbstractValidator<AislarLoteCuarentenaCommand>
{
    public AislarLoteCuarentenaCommandValidator()
    {
        _ = RuleFor(x => x.LoteId).NotEmpty();
        _ = RuleFor(x => x.CantidadAislada).GreaterThan(0);
        _ = RuleFor(x => x.MotivoAislamiento).NotEmpty().MaximumLength(50);
        _ = RuleFor(x => x.EstadoResolucion).NotEmpty().Must(EstadoResolucionCuarentena.EsValido)
            .WithMessage($"EstadoResolucion must be one of: {string.Join(", ", EstadoResolucionCuarentena.Validos)}.");
    }
}
