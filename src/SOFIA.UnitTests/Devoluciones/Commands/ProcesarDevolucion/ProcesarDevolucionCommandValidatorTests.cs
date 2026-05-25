using FluentAssertions;
using SOFIA.Application.Devoluciones.Commands.ProcesarDevolucion;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Devoluciones.Commands.ProcesarDevolucion;

public class ProcesarDevolucionCommandValidatorTests
{
    private readonly ProcesarDevolucionCommandValidator _validator;

    public ProcesarDevolucionCommandValidatorTests() => _validator = new ProcesarDevolucionCommandValidator();

    [Fact]
    public void Validator_WhenValidCommand_ShouldNotHaveAnyErrors()
    {
        // Arrange
        var command = new ProcesarDevolucionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "07",
            "Devolución por producto en mal estado",
            [
                new DevolucionDetalleDto(Guid.NewGuid(), 2m, DestinoDevolucion.Cuarentena_DIGEMID)
            ]
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Validator_WhenIdsAreEmpty_ShouldHaveErrors()
    {
        // Arrange
        var command = new ProcesarDevolucionCommand(
            Guid.Empty,
            Guid.Empty,
            "07",
            "Motivo",
            [new DevolucionDetalleDto(Guid.NewGuid(), 1m, DestinoDevolucion.Reingreso_Venta)]
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(command.ComprobanteOrigenId));
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(command.EmpleadoAutorizaId));
    }

    [Fact]
    public void Validator_WhenMotivoSunatOrSustentoAreInvalid_ShouldHaveErrors()
    {
        // Arrange
        var command = new ProcesarDevolucionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "", // Empty Motivo
            new string('a', 256), // Exceeds max length
            [new DevolucionDetalleDto(Guid.NewGuid(), 1m, DestinoDevolucion.Reingreso_Venta)]
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(command.MotivoSunatCatalogo));
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(command.SustentoDescriptivo));
    }

    [Fact]
    public void Validator_WhenDetallesAreInvalid_ShouldHaveErrors()
    {
        // Arrange
        var command = new ProcesarDevolucionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "07",
            "Motivo",
            [
                new DevolucionDetalleDto(Guid.Empty, 0m, DestinoDevolucion.Reingreso_Venta)
            ]
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == "Detalles[0].DetalleVentaId");
        _ = result.Errors.Should().Contain(e => e.PropertyName == "Detalles[0].CantidadDevuelta");
    }

    [Fact]
    public void Validator_WhenDetallesListIsEmpty_ShouldHaveErrors()
    {
        // Arrange
        var command = new ProcesarDevolucionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "07",
            "Motivo",
            []
        );

        // Act
        var result = _validator.Validate(command);

        // Assert
        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(command.Detalles));
    }
}
