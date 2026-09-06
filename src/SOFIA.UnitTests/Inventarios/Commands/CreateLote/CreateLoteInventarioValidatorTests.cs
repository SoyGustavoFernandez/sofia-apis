using FluentAssertions;
using SOFIA.Application.Inventarios.Commands.CreateLote;

namespace SOFIA.UnitTests.Inventarios.Commands.CreateLote;

public class CreateLoteInventarioValidatorTests
{
    private readonly CreateLoteInventarioValidator _validator = new();

    private static CreateLoteInventarioCommand ValidCommand() => new(
        Guid.NewGuid(),
        "LOT-001",
        DateTimeOffset.UtcNow.AddYears(-1),
        DateTimeOffset.UtcNow.AddYears(1));

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid() =>
        _ = _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldPass_WhenManufactureDateIsNull() =>
        _ = _validator.Validate(ValidCommand() with { FechaFabricacion = null }).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldFail_WhenProductoIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { ProductoId = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLoteInventarioCommand.ProductoId));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNumeroLoteEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { NumeroLoteMfr = "" });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLoteInventarioCommand.NumeroLoteMfr));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNumeroLoteTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { NumeroLoteMfr = new string('x', 101) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLoteInventarioCommand.NumeroLoteMfr));
    }

    [Fact]
    public void Validate_ShouldFail_WhenExpirationDateInThePast()
    {
        var result = _validator.Validate(ValidCommand() with { FechaCaducidad = DateTimeOffset.UtcNow.AddYears(-1) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLoteInventarioCommand.FechaCaducidad));
    }

    [Fact]
    public void Validate_ShouldFail_WhenManufactureDateInTheFuture()
    {
        var result = _validator.Validate(ValidCommand() with { FechaFabricacion = DateTimeOffset.UtcNow.AddDays(10) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateLoteInventarioCommand.FechaFabricacion));
    }
}
