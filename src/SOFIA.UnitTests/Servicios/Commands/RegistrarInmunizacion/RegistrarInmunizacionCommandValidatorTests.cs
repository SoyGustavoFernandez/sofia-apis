using FluentAssertions;
using SOFIA.Application.Servicios.Commands.RegistrarInmunizacion;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Servicios.Commands.RegistrarInmunizacion;

public class RegistrarInmunizacionCommandValidatorTests
{
    private readonly RegistrarInmunizacionCommandValidator _validator = new();

    private static RegistrarInmunizacionCommand ValidCommand() => new(
        null, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(),
        "IM", "Deltoides", 0.5m, DateTime.UtcNow, null, ModalidadRegistro.Corriente);

    [Fact]
    public void ValidCommand_ShouldNotHaveAnyValidationErrors()
    {
        var result = _validator.Validate(ValidCommand());

        _ = result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void MissingFechaAdmnFisica_ShouldHaveValidationError()
    {
        var result = _validator.Validate(ValidCommand() with { FechaAdmnFisica = default });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(RegistrarInmunizacionCommand.FechaAdmnFisica));
    }
}
