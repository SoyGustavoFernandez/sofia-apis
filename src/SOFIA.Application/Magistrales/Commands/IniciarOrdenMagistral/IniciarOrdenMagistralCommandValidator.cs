using FluentValidation;

namespace SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;

public class IniciarOrdenMagistralCommandValidator : AbstractValidator<IniciarOrdenMagistralCommand>
{
    public IniciarOrdenMagistralCommandValidator()
    {
        _ = RuleFor(x => x.ProductoResultanteId).NotEmpty();
        _ = RuleFor(x => x.CantidadProducida).NotNull().GreaterThan(0);
        _ = RuleFor(x => x.Consumos).NotEmpty().WithMessage("Order must have at least one ingredient.");
        _ = RuleForEach(x => x.Consumos).ChildRules(insumo =>
        {
            _ = insumo.RuleFor(i => i.InventarioSucursalId).NotEmpty();
            _ = insumo.RuleFor(i => i.CantidadConsumida).GreaterThan(0);
        });
    }
}
