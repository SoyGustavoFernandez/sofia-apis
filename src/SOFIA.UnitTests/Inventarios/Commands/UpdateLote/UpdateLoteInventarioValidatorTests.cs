using FluentAssertions;
using SOFIA.Application.Inventarios.Commands.UpdateLote;

namespace SOFIA.UnitTests.Inventarios.Commands.UpdateLote;

public class UpdateLoteInventarioValidatorTests
{
    private readonly UpdateLoteInventarioValidator _validator = new();

    private static UpdateLoteInventarioCommand ValidCommand() => new(
        Guid.NewGuid(),
        "LOT-001",
        DateTimeOffset.UtcNow.AddYears(-1),
        DateTimeOffset.UtcNow.AddYears(1));

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid() =>
        _ = _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldFail_WhenIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLoteInventarioCommand.Id));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNumeroLoteEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { NumeroLoteMfr = "" });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLoteInventarioCommand.NumeroLoteMfr));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNumeroLoteTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { NumeroLoteMfr = new string('x', 101) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLoteInventarioCommand.NumeroLoteMfr));
    }

    [Fact]
    public void Validate_ShouldFail_WhenExpirationDateInThePast()
    {
        var result = _validator.Validate(ValidCommand() with { FechaCaducidad = DateTimeOffset.UtcNow.AddYears(-1) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLoteInventarioCommand.FechaCaducidad));
    }

    [Fact]
    public void Validate_ShouldFail_WhenManufactureDateInTheFuture()
    {
        var result = _validator.Validate(ValidCommand() with { FechaFabricacion = DateTimeOffset.UtcNow.AddDays(10) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateLoteInventarioCommand.FechaFabricacion));
    }
}
