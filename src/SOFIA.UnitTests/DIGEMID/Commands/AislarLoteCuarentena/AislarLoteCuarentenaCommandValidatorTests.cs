using FluentAssertions;
using SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;

namespace SOFIA.UnitTests.DIGEMID.Commands.AislarLoteCuarentena;

public class AislarLoteCuarentenaCommandValidatorTests
{
    private readonly AislarLoteCuarentenaCommandValidator _validator = new();

    private static AislarLoteCuarentenaCommand ValidCommand() =>
        new(Guid.NewGuid(), null, 3m, "Producto vencido", "Pendiente");

    [Fact]
    public void Validate_ShouldPass_WhenCommandIsValid() =>
        _ = _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldFail_WhenLoteIdIsEmpty() =>
        _ = _validator.Validate(ValidCommand() with { LoteId = Guid.Empty }).IsValid.Should().BeFalse();

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_ShouldFail_WhenCantidadIsNotPositive(decimal cantidad) =>
        _ = _validator.Validate(ValidCommand() with { CantidadAislada = cantidad }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ShouldFail_WhenMotivoExceedsMaxLength() =>
        _ = _validator.Validate(ValidCommand() with { MotivoAislamiento = new string('a', 51) }).IsValid.Should().BeFalse();

    [Fact]
    public void Validate_ShouldFail_WhenEstadoResolucionIsEmpty() =>
        _ = _validator.Validate(ValidCommand() with { EstadoResolucion = "" }).IsValid.Should().BeFalse();
}
