using FluentValidation.TestHelper;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;

namespace SOFIA.UnitTests.Empresas.Commands.RegistrarEmpresa;

public class RegistrarEmpresaCommandValidatorTests
{
    private readonly RegistrarEmpresaCommandValidator _validator = new();

    private static RegistrarEmpresaCommand ValidCommand() => new()
    {
        NombreEmpresa = "Farmacia Salud",
        Usuario = "admin",
        Password = "Segura123"
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

    [Fact]
    public void Validate_ShouldPass_WhenRUCIsNull()
    {
        var result = _validator.TestValidate(ValidCommand() with { RUC = null });
        result.ShouldNotHaveValidationErrorFor(x => x.RUC);
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
