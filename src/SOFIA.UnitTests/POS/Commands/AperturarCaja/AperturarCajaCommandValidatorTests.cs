using FluentAssertions;
using SOFIA.Application.POS.Commands.AperturarCaja;

namespace SOFIA.UnitTests.POS.Commands.AperturarCaja;

public class AperturarCajaCommandValidatorTests
{
    private readonly AperturarCajaCommandValidator _validator = new();

    [Theory]
    [InlineData(0)]
    [InlineData(150.5)]
    public void Validate_ShouldPass_WhenMontoIsNotNegative(decimal monto) =>
        _ = _validator.Validate(new AperturarCajaCommand(monto)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldFail_WhenMontoIsNegative() =>
        _ = _validator.Validate(new AperturarCajaCommand(-0.01m)).IsValid.Should().BeFalse();
}
