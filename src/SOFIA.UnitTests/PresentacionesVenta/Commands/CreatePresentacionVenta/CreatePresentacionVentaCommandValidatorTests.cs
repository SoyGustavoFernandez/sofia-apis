using FluentAssertions;
using SOFIA.Application.PresentacionesVenta.Commands.CreatePresentacionVenta;

namespace SOFIA.UnitTests.PresentacionesVenta.Commands.CreatePresentacionVenta;

public class CreatePresentacionVentaCommandValidatorTests
{
    private readonly CreatePresentacionVentaCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        var command = new CreatePresentacionVentaCommand(Guid.NewGuid(), Guid.NewGuid(), 45m);

        var result = _validator.Validate(command);

        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenProductoIdIsEmpty()
    {
        var command = new CreatePresentacionVentaCommand(Guid.Empty, Guid.NewGuid(), 45m);

        var result = _validator.Validate(command);

        _ = result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenUnidadVentaIdIsEmpty()
    {
        var command = new CreatePresentacionVentaCommand(Guid.NewGuid(), Guid.Empty, 45m);

        var result = _validator.Validate(command);

        _ = result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenPrecioVentaIsNegative()
    {
        var command = new CreatePresentacionVentaCommand(Guid.NewGuid(), Guid.NewGuid(), -1m);

        var result = _validator.Validate(command);

        _ = result.IsValid.Should().BeFalse();
    }
}
