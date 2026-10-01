using FluentValidation.TestHelper;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;

namespace SOFIA.UnitTests.Empresas.Commands.RegistrarEmpresa;

public class RegistrarEmpresaCommandValidatorTests
{
    private readonly RegistrarEmpresaCommandValidator _validator = new();

    private static RegistrarEmpresaCommand ValidCommand() => new()
    {
        NombreEmpresa = "Farmacia Salud",
        RUC = "20123456789",
        Usuario = "admin",
        Password = "Segura123",
        AdminNombres = "Ana Maria",
        AdminApellidoPaterno = "Quispe",
        AdminEmail = "ana.quispe@farmacia.pe"
    };

    [Fact]
    public void Validate_ShouldPass_WhenOnlyRequiredFieldsProvided()
    {
        var result = _validator.TestValidate(ValidCommand());
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenNombreEmpresaIsEmpty(string nombre)
    {
        var result = _validator.TestValidate(ValidCommand() with { NombreEmpresa = nombre });
        _ = result.ShouldHaveValidationErrorFor(x => x.NombreEmpresa);
    }

    [Theory]
    [InlineData("")]
    [InlineData("ab")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenUsuarioIsTooShortOrEmpty(string usuario)
    {
        var result = _validator.TestValidate(ValidCommand() with { Usuario = usuario });
        _ = result.ShouldHaveValidationErrorFor(x => x.Usuario);
    }

    [Theory]
    [InlineData("")]
    [InlineData("1234567")]
    public void Validate_ShouldFail_WhenPasswordIsTooShortOrEmpty(string password)
    {
        var result = _validator.TestValidate(ValidCommand() with { Password = password });
        _ = result.ShouldHaveValidationErrorFor(x => x.Password);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenRUCIsEmpty(string ruc)
    {
        var result = _validator.TestValidate(ValidCommand() with { RUC = ruc });
        _ = result.ShouldHaveValidationErrorFor(x => x.RUC);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenAdminNombresIsEmpty(string nombres)
    {
        var result = _validator.TestValidate(ValidCommand() with { AdminNombres = nombres });
        _ = result.ShouldHaveValidationErrorFor(x => x.AdminNombres);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAdminNombresExceeds75Characters()
    {
        var result = _validator.TestValidate(ValidCommand() with { AdminNombres = new string('a', 76) });
        _ = result.ShouldHaveValidationErrorFor(x => x.AdminNombres);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_ShouldFail_WhenAdminApellidoPaternoIsEmpty(string apellido)
    {
        var result = _validator.TestValidate(ValidCommand() with { AdminApellidoPaterno = apellido });
        _ = result.ShouldHaveValidationErrorFor(x => x.AdminApellidoPaterno);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAdminApellidoPaternoExceeds75Characters()
    {
        var result = _validator.TestValidate(ValidCommand() with { AdminApellidoPaterno = new string('a', 76) });
        _ = result.ShouldHaveValidationErrorFor(x => x.AdminApellidoPaterno);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("Rojas")]
    public void Validate_ShouldPass_WhenAdminApellidoMaternoIsOmittedOrValid(string? apellido)
    {
        var result = _validator.TestValidate(ValidCommand() with { AdminApellidoMaterno = apellido });
        result.ShouldNotHaveValidationErrorFor(x => x.AdminApellidoMaterno);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAdminApellidoMaternoExceeds75Characters()
    {
        var result = _validator.TestValidate(ValidCommand() with { AdminApellidoMaterno = new string('a', 76) });
        _ = result.ShouldHaveValidationErrorFor(x => x.AdminApellidoMaterno);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("no-es-un-correo")]
    public void Validate_ShouldFail_WhenAdminEmailIsEmptyOrInvalid(string email)
    {
        var result = _validator.TestValidate(ValidCommand() with { AdminEmail = email });
        _ = result.ShouldHaveValidationErrorFor(x => x.AdminEmail);
    }

    [Fact]
    public void Validate_ShouldFail_WhenAdminEmailExceedsMaxLength()
    {
        var email = $"{new string('a', 250)}@x.pe";
        var result = _validator.TestValidate(ValidCommand() with { AdminEmail = email });
        _ = result.ShouldHaveValidationErrorFor(x => x.AdminEmail);
    }

    [Theory]
    [InlineData("1234567890")]     // 10 dígitos
    [InlineData("123456789012")]   // 12 dígitos
    [InlineData("1234567890A")]    // no numérico
    public void Validate_ShouldFail_WhenRUCProvidedButInvalid(string ruc)
    {
        var result = _validator.TestValidate(ValidCommand() with { RUC = ruc });
        _ = result.ShouldHaveValidationErrorFor(x => x.RUC);
    }

    [Fact]
    public void Validate_ShouldPass_WhenRUCIsValid()
    {
        var result = _validator.TestValidate(ValidCommand() with { RUC = "12345678901" });
        result.ShouldNotHaveValidationErrorFor(x => x.RUC);
    }
}
