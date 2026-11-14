using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Plataforma.Domain;

public class PlataformaConfiguracionPagoTests
{
    internal static readonly byte[] Png = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A, 0x00, 0x00];
    internal static readonly byte[] Jpeg = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

    [Fact]
    public void Create_ShouldSucceed_WithValidData()
    {
        var result = PlataformaConfiguracionPago.Create("51987654321", "  Plan mensual  ");

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.WhatsappNumero.Should().Be("51987654321");
        _ = result.Value.Instrucciones.Should().Be("Plan mensual");
        _ = result.Value.QrImagen.Should().BeNull();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("abc")]
    [InlineData("+51987654321")]
    [InlineData("51 987 654 321")]
    [InlineData("1234567")]
    [InlineData("1234567890123456")]
    public void Create_ShouldFail_WhenWhatsappNumeroIsInvalid(string numero)
    {
        var result = PlataformaConfiguracionPago.Create(numero, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ConfiguracionPago.WhatsappNumero");
    }

    [Fact]
    public void Create_ShouldStoreYapeNumero_WhenProvided()
    {
        var result = PlataformaConfiguracionPago.Create("51987654321", null, "987654321");

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.YapeNumero.Should().Be("987654321");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_ShouldClearYapeNumero_WhenBlank(string? yapeNumero)
    {
        var configuracion = PlataformaConfiguracionPago.Create("51987654321", null, "987654321").Value!;

        var result = configuracion.Update("51987654321", null, yapeNumero);

        _ = result.IsSuccess.Should().BeTrue();
        _ = configuracion.YapeNumero.Should().BeNull();
    }

    [Theory]
    [InlineData("98765432")]
    [InlineData("9876543210")]
    [InlineData("987 654 321")]
    [InlineData("51987654321")]
    [InlineData("abcdefghi")]
    public void Update_ShouldFailAndKeepValues_WhenYapeNumeroIsInvalid(string yapeNumero)
    {
        var configuracion = PlataformaConfiguracionPago.Create("51987654321", null, "987654321").Value!;

        var result = configuracion.Update("51999888777", null, yapeNumero);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ConfiguracionPago.YapeNumero");
        _ = configuracion.WhatsappNumero.Should().Be("51987654321");
        _ = configuracion.YapeNumero.Should().Be("987654321");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Update_ShouldStoreNullInstrucciones_WhenBlank(string? instrucciones)
    {
        var configuracion = PlataformaConfiguracionPago.Create("51987654321", "Texto").Value!;

        var result = configuracion.Update("51987654321", instrucciones);

        _ = result.IsSuccess.Should().BeTrue();
        _ = configuracion.Instrucciones.Should().BeNull();
    }

    [Fact]
    public void Update_ShouldFailAndKeepValues_WhenInstruccionesTooLong()
    {
        var configuracion = PlataformaConfiguracionPago.Create("51987654321", "Texto").Value!;

        var result = configuracion.Update("51999888777", new string('a', PlataformaConfiguracionPago.MaxInstruccionesLength + 1));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ConfiguracionPago.Instrucciones");
        _ = configuracion.WhatsappNumero.Should().Be("51987654321");
        _ = configuracion.Instrucciones.Should().Be("Texto");
    }

    [Fact]
    public void CambiarQr_ShouldDetectPng()
    {
        var configuracion = PlataformaConfiguracionPago.Create("51987654321", null).Value!;

        var result = configuracion.CambiarQr(Png);

        _ = result.IsSuccess.Should().BeTrue();
        _ = configuracion.QrContentType.Should().Be("image/png");
        _ = configuracion.QrImagen.Should().Equal(Png);
    }

    [Fact]
    public void CambiarQr_ShouldDetectJpeg()
    {
        var configuracion = PlataformaConfiguracionPago.Create("51987654321", null).Value!;

        var result = configuracion.CambiarQr(Jpeg);

        _ = result.IsSuccess.Should().BeTrue();
        _ = configuracion.QrContentType.Should().Be("image/jpeg");
    }

    [Theory]
    [InlineData("<svg xmlns='http://www.w3.org/2000/svg'><script>alert(1)</script></svg>")]
    [InlineData("GIF89a-not-allowed")]
    [InlineData("<html><body>hola</body></html>")]
    public void CambiarQr_ShouldFailAndKeepPreviousQr_WhenContentIsNotPngOrJpeg(string contenido)
    {
        var configuracion = PlataformaConfiguracionPago.Create("51987654321", null).Value!;
        _ = configuracion.CambiarQr(Png);

        var result = configuracion.CambiarQr(System.Text.Encoding.UTF8.GetBytes(contenido));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ConfiguracionPago.Qr.Formato");
        _ = configuracion.QrImagen.Should().Equal(Png);
        _ = configuracion.QrContentType.Should().Be("image/png");
    }

    [Fact]
    public void CambiarQr_ShouldFail_WhenEmpty()
    {
        var configuracion = PlataformaConfiguracionPago.Create("51987654321", null).Value!;

        var result = configuracion.CambiarQr([]);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ConfiguracionPago.Qr.Tamano");
    }

    [Fact]
    public void CambiarQr_ShouldFail_WhenLargerThanLimit()
    {
        var configuracion = PlataformaConfiguracionPago.Create("51987654321", null).Value!;
        var imagen = new byte[PlataformaConfiguracionPago.MaxQrBytes + 1];
        Png.CopyTo(imagen, 0);

        var result = configuracion.CambiarQr(imagen);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ConfiguracionPago.Qr.Tamano");
    }
}
