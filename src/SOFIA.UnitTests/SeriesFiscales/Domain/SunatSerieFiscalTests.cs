using FluentAssertions;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.SeriesFiscales.Domain;

public class SunatSerieFiscalTests
{
    private readonly Guid _sucursalId = Guid.NewGuid();

    [Theory]
    [InlineData(TipoComprobante.Boleta, "B001", true)]
    [InlineData(TipoComprobante.Boleta, "F001", false)]
    [InlineData(TipoComprobante.Factura, "F001", true)]
    [InlineData(TipoComprobante.Factura, "B001", false)]
    [InlineData(TipoComprobante.NotaCredito, "BC01", true)]
    [InlineData(TipoComprobante.NotaCredito, "FC01", true)]
    [InlineData(TipoComprobante.NotaCredito, "PC01", false)]
    [InlineData(TipoComprobante.Proforma, "P001", true)]
    [InlineData(TipoComprobante.Proforma, "0001", true)]
    [InlineData(TipoComprobante.Boleta, "b001", false)]
    [InlineData(TipoComprobante.Boleta, "B01", false)]
    [InlineData(TipoComprobante.Boleta, "B0001", false)]
    [InlineData(TipoComprobante.Boleta, "B-01", false)]
    public void EsPrefijoValido_ShouldApplySunatRules(TipoComprobante tipo, string prefijo, bool esperado) =>
        _ = SunatSerieFiscal.EsPrefijoValido(tipo, prefijo).Should().Be(esperado);

    [Fact]
    public void Create_ShouldSucceed_WhenDataIsValid()
    {
        var result = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B001", 0, SunatSerieFiscal.EstadoActiva);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.EsActiva.Should().BeTrue();
    }

    [Theory]
    [InlineData(TipoComprobante.Ticket)]
    [InlineData(TipoComprobante.NotaDebito)]
    public void Create_ShouldFail_WhenTypeIsNotConfigurable(TipoComprobante tipo)
    {
        var result = SunatSerieFiscal.Create(_sucursalId, tipo, "T001", 0, SunatSerieFiscal.EstadoActiva);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("SunatSerieFiscal.TipoComprobante");
    }

    [Fact]
    public void Create_ShouldFail_WhenPrefixDoesNotMatchType()
    {
        var result = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Factura, "B001", 0, SunatSerieFiscal.EstadoActiva);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("SunatSerieFiscal.PrefijoSerie");
    }

    [Fact]
    public void Create_ShouldFail_WhenCorrelativeIsNegative()
    {
        var result = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B001", -1, SunatSerieFiscal.EstadoActiva);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("SunatSerieFiscal.CorrelativoActual");
    }

    [Fact]
    public void Create_ShouldFail_WhenStatusIsUnknown()
    {
        var result = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B001", 0, "Pausada");

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("SunatSerieFiscal.EstadoSerie");
    }

    [Fact]
    public void Update_ShouldChangeStatus_WhenDataIsValid()
    {
        var serie = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B001", 3, SunatSerieFiscal.EstadoActiva).Value!;

        var result = serie.Update(_sucursalId, TipoComprobante.Boleta, "B001", 3, SunatSerieFiscal.EstadoInactiva);

        _ = result.IsSuccess.Should().BeTrue();
        _ = serie.EsActiva.Should().BeFalse();
    }

    [Fact]
    public void CambiaNumeracion_ShouldBeFalse_WhenOnlyStatusDiffers()
    {
        var serie = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B001", 3, SunatSerieFiscal.EstadoActiva).Value!;

        _ = serie.CambiaNumeracion(_sucursalId, TipoComprobante.Boleta, "B001", 3).Should().BeFalse();
    }

    [Fact]
    public void CambiaNumeracion_ShouldBeTrue_WhenPrefixCorrelativeTypeOrBranchDiffers()
    {
        var serie = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B001", 3, SunatSerieFiscal.EstadoActiva).Value!;

        _ = serie.CambiaNumeracion(_sucursalId, TipoComprobante.Boleta, "B002", 3).Should().BeTrue();
        _ = serie.CambiaNumeracion(_sucursalId, TipoComprobante.Boleta, "B001", 0).Should().BeTrue();
        _ = serie.CambiaNumeracion(_sucursalId, TipoComprobante.NotaCredito, "B001", 3).Should().BeTrue();
        _ = serie.CambiaNumeracion(Guid.NewGuid(), TipoComprobante.Boleta, "B001", 3).Should().BeTrue();
    }
}
