using FluentValidation.TestHelper;
using SOFIA.Application.Ventas.Commands.CompletarVenta;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Commands.CompletarVenta;

public class CompletarVentaCommandValidatorTests
{
    private readonly CompletarVentaCommandValidator _validator;

    public CompletarVentaCommandValidatorTests() => _validator = new CompletarVentaCommandValidator();

    [Fact]
    public void Should_Have_Error_When_VentaId_Is_Empty()
    {
        // Arrange
        var command = new CompletarVentaCommand(Guid.Empty, [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(x => x.VentaId);
    }

    [Fact]
    public void Should_Have_Error_When_Pagos_Is_Empty()
    {
        // Arrange
        var command = new CompletarVentaCommand(Guid.NewGuid(), []);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(x => x.Pagos)
              .WithErrorMessage("A sale must have at least one payment.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Command_Is_Valid()
    {
        // Arrange
        var command = new CompletarVentaCommand(Guid.NewGuid(), [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]);

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }
}
