using FluentAssertions;
using FluentValidation.TestHelper;
using SOFIA.Application.Plataforma.Commands.GuardarConfiguracionPago;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Plataforma.Commands;

public class GuardarConfiguracionPagoCommandValidatorTests
{
    private readonly GuardarConfiguracionPagoCommandValidator _validator = new();

    [Fact]
    public void Validate_ShouldPass_WithOnlyWhatsappNumero()
    {
        var result = _validator.TestValidate(new GuardarConfiguracionPagoCommand { WhatsappNumero = "51987654321" });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("+51987654321")]
    [InlineData("987-654-321")]
    [InlineData("1234567")]
    [InlineData("1234567890123456")]
    public void Validate_ShouldFail_WhenWhatsappNumeroIsInvalid(string numero)
    {
        var result = _validator.TestValidate(new GuardarConfiguracionPagoCommand { WhatsappNumero = numero });
        _ = result.ShouldHaveValidationErrorFor(x => x.WhatsappNumero);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("987654321")]
    public void Validate_ShouldPass_WhenYapeNumeroIsBlankOrNineDigits(string? yapeNumero)
    {
        var result = _validator.TestValidate(new GuardarConfiguracionPagoCommand { WhatsappNumero = "51987654321", YapeNumero = yapeNumero });
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("98765432")]
    [InlineData("9876543210")]
    [InlineData("987-654-321")]
    public void Validate_ShouldFail_WhenYapeNumeroIsInvalid(string yapeNumero)
    {
        var result = _validator.TestValidate(new GuardarConfiguracionPagoCommand { WhatsappNumero = "51987654321", YapeNumero = yapeNumero });
        _ = result.ShouldHaveValidationErrorFor(x => x.YapeNumero);
    }

    [Fact]
    public void Validate_ShouldFail_WhenInstruccionesTooLong()
    {
        var command = new GuardarConfiguracionPagoCommand
        {
            WhatsappNumero = "51987654321",
            Instrucciones = new string('a', PlataformaConfiguracionPago.MaxInstruccionesLength + 1)
        };

        var result = _validator.TestValidate(command);
        _ = result.ShouldHaveValidationErrorFor(x => x.Instrucciones);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(PlataformaConfiguracionPago.MaxQrBytes + 1)]
    public void Validate_ShouldFail_WhenQrSizeIsOutOfRange(int bytes)
    {
        var result = _validator.TestValidate(new GuardarConfiguracionPagoCommand { WhatsappNumero = "51987654321", QrImagen = new byte[bytes] });
        _ = result.IsValid.Should().BeFalse();
    }
}
