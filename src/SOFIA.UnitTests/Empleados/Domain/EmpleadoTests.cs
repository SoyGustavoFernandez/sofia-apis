using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Empleados.Domain;

public class EmpleadoTests
{
    private static readonly Guid SucursalId = Guid.NewGuid();

    [Fact]
    public void Create_ShouldSucceed_WhenAllRequiredFieldsPresent()
    {
        var result = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez", "LIC-1");

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Nombres.Should().Be("Ana");
        _ = result.Value.Apellido_Paterno.Should().Be("Pérez");
        _ = result.Value.Apellido_Materno.Should().Be("Gómez");
        _ = result.Value.Licencia_Prof.Should().Be("LIC-1");
        _ = result.Value.Sucursal_Base_ID.Should().Be(SucursalId);
        _ = result.Value.Nombre_Completo.Should().Be("Ana Pérez Gómez");
    }

    [Fact]
    public void Create_ShouldSucceed_WhenLicenciaOmitted()
    {
        var result = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez");

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Licencia_Prof.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldFail_WhenSucursalEmpty()
    {
        var result = Empleado.Create(Guid.Empty, "Ana", "Pérez", "Gómez");

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.Sucursal");
    }

    [Theory]
    [InlineData("", "Empleado.Nombres")]
    [InlineData("   ", "Empleado.Nombres")]
    public void Create_ShouldFail_WhenNombresMissing(string nombres, string code)
    {
        var result = Empleado.Create(SucursalId, nombres, "Pérez", "Gómez");

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be(code);
    }

    [Fact]
    public void Create_ShouldFail_WhenApellidoPaternoMissing()
    {
        var result = Empleado.Create(SucursalId, "Ana", " ", "Gómez");

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.ApellidoPaterno");
    }

    [Fact]
    public void Create_ShouldFail_WhenApellidoMaternoMissing()
    {
        var result = Empleado.Create(SucursalId, "Ana", "Pérez", "");

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.ApellidoMaterno");
    }

    [Fact]
    public void Update_ShouldMutateFields_WhenValid()
    {
        var empleado = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez").Value!;
        var newSucursal = Guid.NewGuid();

        var result = empleado.Update(newSucursal, "Ana María", "Pereira", "González", "LIC-2", null);

        _ = result.IsSuccess.Should().BeTrue();
        _ = empleado.Nombres.Should().Be("Ana María");
        _ = empleado.Apellido_Paterno.Should().Be("Pereira");
        _ = empleado.Apellido_Materno.Should().Be("González");
        _ = empleado.Licencia_Prof.Should().Be("LIC-2");
        _ = empleado.Sucursal_Base_ID.Should().Be(newSucursal);
    }

    [Fact]
    public void Update_ShouldFail_AndNotMutate_WhenInvalid()
    {
        var empleado = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez").Value!;

        var result = empleado.Update(SucursalId, "", "Pérez", "Gómez", null, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = empleado.Nombres.Should().Be("Ana");
    }
}
