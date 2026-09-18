using FluentAssertions;
using SOFIA.Application.PresentacionesVenta.Commands.UpdatePresentacionVenta;

namespace SOFIA.UnitTests.PresentacionesVenta.Commands.UpdatePresentacionVenta;

public class UpdatePresentacionVentaCommandValidatorTests
{
    private readonly UpdatePresentacionVentaCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid()
    {
        var command = new UpdatePresentacionVentaCommand(Guid.NewGuid(), Guid.NewGuid(), 45m);

        var result = _validator.Validate(command);

        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validate_ShouldFail_WhenIdIsEmpty()
    {
        var command = new UpdatePresentacionVentaCommand(Guid.Empty, Guid.NewGuid(), 45m);

        var result = _validator.Validate(command);

        _ = result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenUnidadVentaIdIsEmpty()
    {
        var command = new UpdatePresentacionVentaCommand(Guid.NewGuid(), Guid.Empty, 45m);

        var result = _validator.Validate(command);

        _ = result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldFail_WhenPrecioVentaIsNegative()
    {
        var command = new UpdatePresentacionVentaCommand(Guid.NewGuid(), Guid.NewGuid(), -1m);

        var result = _validator.Validate(command);

        _ = result.IsValid.Should().BeFalse();
    }
}
