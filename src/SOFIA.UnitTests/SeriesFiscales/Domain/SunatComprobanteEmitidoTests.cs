using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.SeriesFiscales.Domain;

public class SunatComprobanteEmitidoTests
{
    private static SunatComprobanteEmitido NoEnviado() =>
        SunatComprobanteEmitido.Create(
            Guid.NewGuid(), Guid.NewGuid(), 1,
            TipoDocumentoIdentidad.Dni, "45781236", "Juan Perez",
            84.75m, 0, 15.25m, 100m,
            null, EstadoComprobanteSunat.NoEnviado, null, null, null).Value!;

    [Fact]
    public void Create_ShouldKeepTheNotSentStatus_WithoutSunatArtifacts()
    {
        var comprobante = NoEnviado();

        _ = comprobante.EstadoAceptacion.Should().Be(EstadoComprobanteSunat.NoEnviado);
        _ = comprobante.HashFirmaDigital.Should().BeNull();
        _ = comprobante.RutaArchivoXml.Should().BeNull();
        _ = comprobante.RutaArchivoCdr.Should().BeNull();
        _ = comprobante.UrlPublicaVerificacion.Should().BeNull();
    }

    [Fact]
    public void Anular_ShouldMarkTheDocumentAsVoided()
    {
        var comprobante = NoEnviado();

        var result = comprobante.Anular();

        _ = result.IsSuccess.Should().BeTrue();
        _ = comprobante.EstadoAceptacion.Should().Be(EstadoComprobanteSunat.Anulado);
    }

    [Fact]
    public void Anular_ShouldFail_WhenTheDocumentIsAlreadyVoided()
    {
        var comprobante = NoEnviado();
        _ = comprobante.Anular();

        var result = comprobante.Anular();

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("SunatComprobanteEmitido.YaAnulado");
        _ = comprobante.EstadoAceptacion.Should().Be(EstadoComprobanteSunat.Anulado);
    }

    [Fact]
    public void EstadoComprobanteSunat_ShouldFitTheVarchar20Column() =>
        _ = new[] { EstadoComprobanteSunat.NoEnviado, EstadoComprobanteSunat.Aceptado, EstadoComprobanteSunat.Rechazado, EstadoComprobanteSunat.Anulado }
            .Should().OnlyContain(e => e.Length <= 20);
}
