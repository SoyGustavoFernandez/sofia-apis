using FluentAssertions;
using SOFIA.Application.Empleados.Commands.CreateEmpleado;

namespace SOFIA.UnitTests.Empleados.Commands.CreateEmpleado;

public class CreateEmpleadoCommandValidatorTests
{
    private readonly CreateEmpleadoCommandValidator _validator = new();

    private static CreateEmpleadoCommand ValidCommand() => new()
    {
        Sucursal_Base_ID = Guid.NewGuid(),
        Nombres = "Ana",
        Apellido_Paterno = "Pérez",
        Apellido_Materno = "Gómez",
        Licencia_Prof = "LIC-1",
    };

    [Fact]
    public void Validate_ShouldPass_WhenCommandValid() => _ = _validator.Validate(ValidCommand()).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldPass_WhenLicenciaNull() => _ = _validator.Validate(ValidCommand() with { Licencia_Prof = null }).IsValid.Should().BeTrue();

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("ana@farmacia.pe")]
    public void Validate_ShouldPass_WhenEmailEmptyOrValid(string? email) => _ = _validator.Validate(ValidCommand() with { Email = email }).IsValid.Should().BeTrue();

    [Fact]
    public void Validate_ShouldFail_WhenEmailMalformed()
    {
        var result = _validator.Validate(ValidCommand() with { Email = "no-es-correo" });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEmpleadoCommand.Email));
    }

    [Fact]
    public void Validate_ShouldFail_WhenEmailTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { Email = new string('a', 250) + "@farmacia.pe" });

        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEmpleadoCommand.Email));
    }

    [Fact]
    public void Validate_ShouldFail_WhenSucursalEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { Sucursal_Base_ID = Guid.Empty });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEmpleadoCommand.Sucursal_Base_ID));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenNombresEmpty(string nombres)
    {
        var result = _validator.Validate(ValidCommand() with { Nombres = nombres });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEmpleadoCommand.Nombres));
    }

    [Fact]
    public void Validate_ShouldFail_WhenApellidoPaternoTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { Apellido_Paterno = new string('x', 76) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEmpleadoCommand.Apellido_Paterno));
    }

    [Fact]
    public void Validate_ShouldFail_WhenApellidoMaternoEmpty()
    {
        var result = _validator.Validate(ValidCommand() with { Apellido_Materno = "" });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEmpleadoCommand.Apellido_Materno));
    }

    [Fact]
    public void Validate_ShouldFail_WhenLicenciaTooLong()
    {
        var result = _validator.Validate(ValidCommand() with { Licencia_Prof = new string('x', 51) });

        _ = result.IsValid.Should().BeFalse();
        _ = result.Errors.Should().Contain(e => e.PropertyName == nameof(CreateEmpleadoCommand.Licencia_Prof));
    }
}
