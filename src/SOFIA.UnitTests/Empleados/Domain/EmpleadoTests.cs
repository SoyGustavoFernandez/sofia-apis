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

        var result = empleado.Update(newSucursal, "Ana María", "Pereira", "González", "LIC-2", null, null);

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

        var result = empleado.Update(SucursalId, "", "Pérez", "Gómez", null, null, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = empleado.Nombres.Should().Be("Ana");
    }

    [Fact]
    public void Create_ShouldStoreTrimmedLowerCaseEmail_WhenEmailProvided()
    {
        var result = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez", email: "  Ana.Perez@Farmacia.PE ");

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Email.Should().Be("ana.perez@farmacia.pe");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_ShouldLeaveEmailEmpty_WhenEmailIsBlank(string? email)
    {
        var result = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez", email: email);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Email.Should().BeNull();
    }

    [Theory]
    [InlineData("ana")]
    [InlineData("ana@farmacia")]
    [InlineData("ana perez@farmacia.pe")]
    [InlineData("ana@@farmacia.pe")]
    public void Create_ShouldFail_WhenEmailIsMalformed(string email)
    {
        var result = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez", email: email);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.Email.Invalido");
    }

    [Fact]
    public void Create_ShouldFail_WhenEmailExceedsMaxLength()
    {
        var email = new string('a', Empleado.EmailMaxLength - "@farmacia.pe".Length + 1) + "@farmacia.pe";

        var result = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez", email: email);

        _ = result.Error.Code.Should().Be("Empleado.Email.Invalido");
    }

    [Fact]
    public void Update_ShouldReplaceAndClearEmail()
    {
        var empleado = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez", email: "ana@farmacia.pe").Value!;

        _ = empleado.Update(SucursalId, "Ana", "Pérez", "Gómez", null, null, "Nueva@Farmacia.pe").IsSuccess.Should().BeTrue();
        _ = empleado.Email.Should().Be("nueva@farmacia.pe");

        _ = empleado.Update(SucursalId, "Ana", "Pérez", "Gómez", null, null, " ").IsSuccess.Should().BeTrue();
        _ = empleado.Email.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldFail_AndKeepEmail_WhenEmailIsMalformed()
    {
        var empleado = Empleado.Create(SucursalId, "Ana", "Pérez", "Gómez", email: "ana@farmacia.pe").Value!;

        var result = empleado.Update(SucursalId, "Ana", "Pérez", "Gómez", null, null, "no-es-correo");

        _ = result.Error.Code.Should().Be("Empleado.Email.Invalido");
        _ = empleado.Email.Should().Be("ana@farmacia.pe");
    }
}
