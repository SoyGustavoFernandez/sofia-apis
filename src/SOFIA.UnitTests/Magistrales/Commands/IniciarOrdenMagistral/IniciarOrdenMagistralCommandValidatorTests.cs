using FluentAssertions;
using SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;

namespace SOFIA.UnitTests.Magistrales.Commands.IniciarOrdenMagistral;

public class IniciarOrdenMagistralCommandValidatorTests
{
    private readonly IniciarOrdenMagistralCommandValidator _validator = new();

    private static IniciarOrdenMagistralCommand ValidCommand() =>
        new(Guid.NewGuid(), Guid.NewGuid(), 10, [new(Guid.NewGuid(), 5)]);

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var result = _validator.Validate(ValidCommand());

        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyProductoResultanteId_ShouldHaveValidationError()
    {
        var result = _validator.Validate(ValidCommand() with { ProductoResultanteId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(IniciarOrdenMagistralCommand.ProductoResultanteId));
    }

    [Fact]
    public void EmptyConsumos_ShouldHaveValidationError()
    {
        var result = _validator.Validate(ValidCommand() with { Consumos = [] });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(IniciarOrdenMagistralCommand.Consumos));
    }

    [Fact]
    public void NegativeCantidadConsumida_ShouldHaveValidationError()
    {
        var result = _validator.Validate(ValidCommand() with { Consumos = [new(Guid.NewGuid(), -5)] });

        _ = result.IsValid.Should().BeFalse();
    }
}
