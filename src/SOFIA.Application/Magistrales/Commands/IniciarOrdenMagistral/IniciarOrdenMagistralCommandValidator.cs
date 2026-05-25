using FluentValidation;

namespace SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;

public class IniciarOrdenMagistralCommandValidator : AbstractValidator<IniciarOrdenMagistralCommand>
{
    public IniciarOrdenMagistralCommandValidator()
    {
        _ = RuleFor(x => x.SucursalId).NotEmpty();
        _ = RuleFor(x => x.ProductoResultanteId).NotEmpty();
        _ = RuleFor(x => x.QuimicoPreparadorId).NotEmpty();
        _ = RuleFor(x => x.CantidadProducida).GreaterThan(0).When(x => x.CantidadProducida.HasValue);
        _ = RuleFor(x => x.Consumos).NotEmpty().WithMessage("La orden debe tener al menos un insumo.");
        _ = RuleForEach(x => x.Consumos).ChildRules(insumo =>
        {
            _ = insumo.RuleFor(i => i.InventarioSucursalId).NotEmpty();
            _ = insumo.RuleFor(i => i.CantidadConsumida).GreaterThan(0);
        });
    }
}
