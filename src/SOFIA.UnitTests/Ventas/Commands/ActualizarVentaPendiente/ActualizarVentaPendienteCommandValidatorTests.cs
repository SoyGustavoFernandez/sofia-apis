using FluentValidation.TestHelper;
using SOFIA.Application.Ventas.Commands.ActualizarVentaPendiente;
using SOFIA.Application.Ventas.Commands.CreateVenta;

namespace SOFIA.UnitTests.Ventas.Commands.ActualizarVentaPendiente;

public class ActualizarVentaPendienteCommandValidatorTests
{
    private readonly ActualizarVentaPendienteCommandValidator _validator;

    public ActualizarVentaPendienteCommandValidatorTests() => _validator = new ActualizarVentaPendienteCommandValidator();

    [Fact]
    public void Should_Have_Error_When_VentaId_Is_Empty()
    {
        // Arrange
        var command = new ActualizarVentaPendienteCommand(Guid.Empty, [new CreateVentaDetailDto(Guid.NewGuid(), 1, 10, 5)]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(x => x.VentaId);
    }

    [Fact]
    public void Should_Have_Error_When_Detalles_Is_Empty()
    {
        // Arrange
        var command = new ActualizarVentaPendienteCommand(Guid.NewGuid(), []);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(x => x.Detalles)
              .WithErrorMessage("A sale must have at least one detail.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        // Arrange
        var command = new ActualizarVentaPendienteCommand(Guid.NewGuid(), [new CreateVentaDetailDto(Guid.NewGuid(), 1, 10, 5)]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
