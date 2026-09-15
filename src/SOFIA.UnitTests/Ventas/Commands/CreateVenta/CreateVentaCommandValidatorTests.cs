using FluentValidation.TestHelper;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Enums;

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
            Detalles: [],
            Pagos: [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]
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
            ],
            Pagos: [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]
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
            ],
            Pagos: [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]
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
            ],
            Pagos: [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]
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
            ],
            Pagos: [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Should_Have_Error_When_Pagos_Is_Empty()
    {
        // Arrange
        var command = new CreateVentaCommand(
            ClienteId: Guid.NewGuid(),
            SesionId: Guid.NewGuid(),
            Detalles: [new CreateVentaDetailDto(Guid.NewGuid(), 1, 10, 5)],
            Pagos: []
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor(x => x.Pagos)
              .WithErrorMessage("A sale must have at least one payment.");
    }

    [Fact]
    public void Should_Not_Have_Error_When_Pagos_Is_Empty_And_Estado_Is_Pendiente()
    {
        // Arrange
        var command = new CreateVentaCommand(
            ClienteId: Guid.NewGuid(),
            SesionId: Guid.NewGuid(),
            Detalles: [new CreateVentaDetailDto(Guid.NewGuid(), 1, 10, 5)],
            Pagos: [],
            Estado: EstadoVenta.Pendiente
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        result.ShouldNotHaveValidationErrorFor(x => x.Pagos);
    }

    [Fact]
    public void Should_Have_Error_When_MontoPagado_Is_Less_Than_Or_Equal_To_Zero()
    {
        // Arrange
        var command = new CreateVentaCommand(
            ClienteId: Guid.NewGuid(),
            SesionId: Guid.NewGuid(),
            Detalles: [new CreateVentaDetailDto(Guid.NewGuid(), 1, 10, 5)],
            Pagos: [new CreateVentaPagoDto(MetodoPago.Efectivo, 0, null)]
        );

        // Act
        var result = _validator.TestValidate(command);

        // Assert
        _ = result.ShouldHaveValidationErrorFor("Pagos[0].MontoPagado");
    }
}
