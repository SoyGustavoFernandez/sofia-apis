using FluentAssertions;
using SOFIA.Application.Inventarios.Commands.AdjustStock;

namespace SOFIA.UnitTests.Inventarios.Commands.AdjustStock;

public class AdjustStockCommandValidatorTests
{
    private readonly AdjustStockCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid() =>
        _ = _validator.Validate(new AdjustStockCommand(Guid.NewGuid(), 10)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldPass_WhenQuantityIsZero() =>
        _ = _validator.Validate(new AdjustStockCommand(Guid.NewGuid(), 0)).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldFail_WhenIdEmpty()
    {
        var result = _validator.Validate(new AdjustStockCommand(Guid.Empty, 10));

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(AdjustStockCommand.Id));
    }

    [Fact]
    public void Validate_ShouldFail_WhenQuantityIsNegative()
    {
        var result = _validator.Validate(new AdjustStockCommand(Guid.NewGuid(), -1));

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(AdjustStockCommand.NuevaCantidad));
    }
}
