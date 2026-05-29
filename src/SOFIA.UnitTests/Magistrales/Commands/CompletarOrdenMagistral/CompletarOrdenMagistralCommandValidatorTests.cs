using FluentAssertions;
using SOFIA.Application.Magistrales.Commands.CompletarOrdenMagistral;

namespace SOFIA.UnitTests.Magistrales.Commands.CompletarOrdenMagistral;

public class CompletarOrdenMagistralCommandValidatorTests
{
    private readonly CompletarOrdenMagistralCommandValidator _validator;

    public CompletarOrdenMagistralCommandValidatorTests() => _validator = new CompletarOrdenMagistralCommandValidator();

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        // Arrange
        var command = new CompletarOrdenMagistralCommand(
            Guid.NewGuid(),
            "LOTE-123",
            DateTimeOffset.UtcNow.AddDays(30)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void EmptyOrdenId_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CompletarOrdenMagistralCommand(
            Guid.Empty,
            "LOTE-123",
            DateTimeOffset.UtcNow.AddDays(30)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == "OrdenId");
    }

    [Fact]
    public void EmptyNumeroLote_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CompletarOrdenMagistralCommand(
            Guid.NewGuid(),
            "",
            DateTimeOffset.UtcNow.AddDays(30)
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == "NumeroLoteMfr");
    }

    [Fact]
    public void PastFechaCaducidad_ShouldHaveValidationError()
    {
        // Arrange
        var command = new CompletarOrdenMagistralCommand(
            Guid.NewGuid(),
            "LOTE-123",
            DateTimeOffset.UtcNow.AddDays(-1) // Past date
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == "FechaCaducidad");
    }
}
