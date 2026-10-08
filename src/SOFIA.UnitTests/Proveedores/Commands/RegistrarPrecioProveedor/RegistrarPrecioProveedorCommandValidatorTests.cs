using FluentAssertions;
using SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;

namespace SOFIA.UnitTests.Proveedores.Commands.RegistrarPrecioProveedor;

public class RegistrarPrecioProveedorCommandValidatorTests
{
    private readonly RegistrarPrecioProveedorCommandValidator _validator = new();

    private static RegistrarPrecioProveedorCommand ValidCommand() => new(Guid.NewGuid(), Guid.NewGuid(), 10m, 15m);

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid() =>
        _ = _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Validate_ShouldPass_WhenDiscountIsAtTheLimits(double descuento) =>
        _ = _validator.Validate(ValidCommand() with { DescuentoPorcentaje = (decimal)descuento }).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldPass_WhenPriceIsTheMaximum() =>
        _ = _validator.Validate(ValidCommand() with { PrecioCompra = RegistrarPrecioProveedorCommandValidator.PrecioCompraMaximo }).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenPriceIsNotPositive(double precio)
    {
        var result = _validator.Validate(ValidCommand() with { PrecioCompra = (decimal)precio });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(RegistrarPrecioProveedorCommand.PrecioCompra));
    }

    [Fact]
    public void Validate_ShouldFail_WhenPriceExceedsTheMaximum()
    {
        var result = _validator.Validate(ValidCommand() with { PrecioCompra = RegistrarPrecioProveedorCommandValidator.PrecioCompraMaximo + 1m });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(RegistrarPrecioProveedorCommand.PrecioCompra));
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public void Validate_ShouldFail_WhenDiscountIsOutOfRange(double descuento)
    {
        var result = _validator.Validate(ValidCommand() with { DescuentoPorcentaje = (decimal)descuento });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(RegistrarPrecioProveedorCommand.DescuentoPorcentaje));
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdsAreEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { ProveedorId = Guid.Empty, MedicamentoId = Guid.Empty });

        _ = result.Errors.Select(e => e.PropertyName).Should().Contain([nameof(RegistrarPrecioProveedorCommand.ProveedorId), nameof(RegistrarPrecioProveedorCommand.MedicamentoId)]);
    }
}
