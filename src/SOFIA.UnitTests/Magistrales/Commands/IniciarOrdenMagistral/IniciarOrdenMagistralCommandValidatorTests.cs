using FluentAssertions;
using SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;
using Xunit;

namespace SOFIA.UnitTests.Magistrales.Commands.IniciarOrdenMagistral;

public class IniciarOrdenMagistralCommandValidatorTests
{
    private readonly IniciarOrdenMagistralCommandValidator _validator;

    public IniciarOrdenMagistralCommandValidatorTests()
    {
        _validator = new IniciarOrdenMagistralCommandValidator();
    }

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new IniciarOrdenMagistralCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            new List<InsumoDto>
            {
                new InsumoDto(Guid.NewGuid(), 5)
            }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptySucursalId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new IniciarOrdenMagistralCommand(
            Guid.Empty,
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            new List<InsumoDto> { new InsumoDto(Guid.NewGuid(), 5) }
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "SucursalId");
    }

    [Fact]
    public void EmptyConsumos_ShouldHaveValidationError()
    {
        // Arrange
        var command = new IniciarOrdenMagistralCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            new List<InsumoDto>() // Empty list
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Consumos");
    }

    [Fact]
    public void NegativeCantidadConsumida_ShouldHaveValidationError()
    {
        // Arrange
        var command = new IniciarOrdenMagistralCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            new List<InsumoDto> { new InsumoDto(Guid.NewGuid(), -1) } // Negative quantity
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Consumos[0].CantidadConsumida");
    }
}
