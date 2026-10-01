using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Pacientes.Domain;

public class PacienteClienteTests
{
    private static readonly DateOnly Nacimiento = new(1990, 1, 1);

    [Theory]
    [InlineData(TipoDocumentoIdentidad.Dni, "45781236")]
    [InlineData(TipoDocumentoIdentidad.Ruc, "20212971893")]
    [InlineData(TipoDocumentoIdentidad.CarneExtranjeria, "CE0012345678")]
    [InlineData(TipoDocumentoIdentidad.Pasaporte, "AB12345678901234567C")]
    public void Create_ShouldSucceed_WhenNumberMatchesItsDocumentType(string tipo, string numero)
    {
        var result = PacienteCliente.Create(tipo, numero, "Juan Perez", Nacimiento, null);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.TipoDocumento.Should().Be(tipo);
        _ = result.Value.DocIdentidadGub.Should().Be(numero);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("-")]
    [InlineData("0")]
    [InlineData("DNI")]
    public void Create_ShouldFail_WhenDocumentTypeIsMissingOrUnknown(string? tipo)
    {
        var result = PacienteCliente.Create(tipo, "45781236", "Juan Perez", Nacimiento, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PacienteCliente.TipoDocumento");
    }

    [Theory]
    [InlineData(TipoDocumentoIdentidad.Dni, "4578123")]
    [InlineData(TipoDocumentoIdentidad.Dni, "457812369")]
    [InlineData(TipoDocumentoIdentidad.Dni, "4578123A")]
    [InlineData(TipoDocumentoIdentidad.Ruc, "2021297189")]
    [InlineData(TipoDocumentoIdentidad.Ruc, "2021297189X")]
    [InlineData(TipoDocumentoIdentidad.CarneExtranjeria, "CE00123456789")]
    [InlineData(TipoDocumentoIdentidad.CarneExtranjeria, "CE-001")]
    [InlineData(TipoDocumentoIdentidad.Pasaporte, "AB12345678901234567CD")]
    [InlineData(TipoDocumentoIdentidad.Pasaporte, "AB 123")]
    public void Create_ShouldFail_WhenNumberDoesNotMatchItsDocumentType(string tipo, string numero)
    {
        var result = PacienteCliente.Create(tipo, numero, "Juan Perez", Nacimiento, null);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PacienteCliente.DocIdentidadGub.FormatoInvalido");
    }

    [Fact]
    public void Update_ShouldRequireTheDocumentType_AndStoreIt()
    {
        var paciente = PacienteCliente.Create(TipoDocumentoIdentidad.Dni, "45781236", "Juan Perez", Nacimiento, null).Value!;

        var sinTipo = paciente.Update(null, "45781236", "Juan Perez", Nacimiento, null);
        var conTipo = paciente.Update(TipoDocumentoIdentidad.Ruc, "20212971893", "Juan Perez", Nacimiento, null);

        _ = sinTipo.Error.Code.Should().Be("PacienteCliente.TipoDocumento");
        _ = conTipo.IsSuccess.Should().BeTrue();
        _ = paciente.TipoDocumento.Should().Be(TipoDocumentoIdentidad.Ruc);
        _ = paciente.DocIdentidadGub.Should().Be("20212971893");
    }

    [Theory]
    [InlineData("DNI", TipoDocumentoIdentidad.Dni)]
    [InlineData(" ce ", TipoDocumentoIdentidad.CarneExtranjeria)]
    [InlineData("ruc", TipoDocumentoIdentidad.Ruc)]
    [InlineData("Pasaporte", TipoDocumentoIdentidad.Pasaporte)]
    [InlineData("7", TipoDocumentoIdentidad.Pasaporte)]
    [InlineData("LE", null)]
    [InlineData("", null)]
    [InlineData(null, null)]
    public void TipoDocumentoIdentidadParse_ShouldAcceptCodesAndShortNames(string? valor, string? esperado) =>
        _ = TipoDocumentoIdentidad.Parse(valor).Should().Be(esperado);
}
