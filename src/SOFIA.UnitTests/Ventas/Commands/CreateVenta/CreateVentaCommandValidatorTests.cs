using FluentValidation.TestHelper;
using SOFIA.Application.Ventas.Commands.CreateVenta;

namespace SOFIA.UnitTests.Ventas.Commands.CreateVenta;

public class CreateVentaCommandValidatorTests
{
    private readonly CreateVentaCommandValidator _validator;

    public CreateVentaCommandValidatorTests() => _validator = new CreateVentaCommandValidator();

    [Fact]
    public void Should_Have_Error_When_Detalles_Is_Empty()
    {
        // Arrange
        var command = new CreateVentaCommand(
            ClienteId: Guid.NewGuid(),
            SesionId: Guid.NewGuid(),
            Detalles: []
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(x => x.Detalles)
              .WithErrorMessage("A sale must have at least one detail.");
    }

    [Fact]
    public void Should_Have_Error_When_LoteId_Is_Empty()
    {
        // Arrange
        var command = new CreateVentaCommand(
            ClienteId: Guid.NewGuid(),
            SesionId: Guid.NewGuid(),
            Detalles:
            [
                new CreateVentaDetailDto(Guid.Empty, 1, 10, 5)
            ]
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor("Detalles[0].LoteId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_Have_Error_When_Cantidad_Is_Less_Than_Or_Equal_To_Zero(decimal cantidad)
    {
        // Arrange
        var command = new CreateVentaCommand(
            ClienteId: Guid.NewGuid(),
            SesionId: Guid.NewGuid(),
            Detalles:
            [
                new CreateVentaDetailDto(Guid.NewGuid(), cantidad, 10, 5)
            ]
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor("Detalles[0].Cantidad");
    }

    [Fact]
    public void Should_Have_Error_When_PrecioUnitario_Is_Less_Than_Zero()
    {
        // Arrange
        var command = new CreateVentaCommand(
            ClienteId: Guid.NewGuid(),
            SesionId: Guid.NewGuid(),
            Detalles:
            [
                new CreateVentaDetailDto(Guid.NewGuid(), 1, -1, 5)
            ]
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor("Detalles[0].PrecioUnitario");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        // Arrange
        var command = new CreateVentaCommand(
            ClienteId: Guid.NewGuid(),
            SesionId: Guid.NewGuid(),
            Detalles:
            [
                new CreateVentaDetailDto(Guid.NewGuid(), 1, 10, 5)
            ]
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
