using FluentAssertions;
using SOFIA.Application.Empleados.Commands.UpdateEmpleado;

namespace SOFIA.UnitTests.Empleados.Commands.UpdateEmpleado;

public class UpdateEmpleadoCommandValidatorTests
{
    private readonly UpdateEmpleadoCommandValidator _validator = new();

    private static UpdateEmpleadoCommand ValidCommand() => new()
    {
        Id = Guid.NewGuid(),
        Sucursal_Base_ID = Guid.NewGuid(),
        Nombres = "Ana",
        Apellido_Paterno = "Pérez",
        Apellido_Materno = "Gómez",
    };

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid() => _ = _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldFail_WhenIdEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { Id = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEmpleadoCommand.Id));
    }

    [Fact]
    public void Validate_ShouldFail_WhenSucursalEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { Sucursal_Base_ID = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEmpleadoCommand.Sucursal_Base_ID));
    }

    [Fact]
    public void Validate_ShouldFail_WhenNombresEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { Nombres = "" });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEmpleadoCommand.Nombres));
    }

    [Fact]
    public void Validate_ShouldFail_WhenApellidoMaternoTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { Apellido_Materno = new string('x', 76) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEmpleadoCommand.Apellido_Materno));
    }

    [Fact]
    public void Validate_ShouldFail_WhenLicenciaTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { Licencia_Prof = new string('x', 51) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(UpdateEmpleadoCommand.Licencia_Prof));
    }
}
