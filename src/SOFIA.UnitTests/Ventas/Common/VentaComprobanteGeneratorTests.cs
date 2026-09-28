using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Common;

public class VentaComprobanteGeneratorTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<SunatComprobanteEmitido> _comprobantes = [];
    private readonly Guid _sucursalId = Guid.NewGuid();

    [Theory]
    [InlineData(100, 84.75, 15.25)]
    [InlineData(118, 100, 18)]
    [InlineData(40, 33.90, 6.10)]
    [InlineData(10.01, 8.48, 1.53)]
    [InlineData(0, 0, 0)]
    public void DesglosarIgv_ShouldExtractIgvFromAnIgvInclusiveTotal(decimal total, decimal gravadoEsperado, decimal igvEsperado)
    {
        var (gravado, igv) = VentaComprobanteGenerator.DesglosarIgv(total);

        _ = gravado.Should().Be(gravadoEsperado);
        _ = igv.Should().Be(igvEsperado);
        _ = (gravado + igv).Should().Be(total);
    }

    private void SetupMocks(List<SunatSerieFiscal> series)
    {
        _ = _dbContextMock.Setup(c => c.SUNATSeriesFiscales).Returns(series.BuildMockDbSet().Object);
        var comprobantesMock = _comprobantes.BuildMockDbSet();
        _ = comprobantesMock.Setup(d => d.Add(It.IsAny<SunatComprobanteEmitido>())).Callback<SunatComprobanteEmitido>(_comprobantes.Add);
        _ = _dbContextMock.Setup(c => c.SUNATComprobantesEmitidos).Returns(comprobantesMock.Object);
    }

    private Venta NuevaVenta() =>
        Venta.Create(_sucursalId, Guid.NewGuid(), null, Guid.NewGuid(), [DetalleVenta.Create(Guid.NewGuid(), 1, 100, 60).Value!]).Value!;

    [Fact]
    public async Task EmitirBoletaAsync_ShouldFailWithoutConsumingCorrelative_WhenBranchHasNoActiveBoletaSeries()
    {
        SetupMocks([
            SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B001", 0, SunatSerieFiscal.EstadoInactiva).Value!,
            SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Factura, "F001", 0, SunatSerieFiscal.EstadoActiva).Value!,
        ]);

        var result = await VentaComprobanteGenerator.EmitirBoletaAsync(_dbContextMock.Object, NuevaVenta(), _sucursalId, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.SerieBoleta.NoConfigurada");
        _dbContextMock.Verify(c => c.IncrementarCorrelativoSunatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _ = _comprobantes.Should().BeEmpty();
    }

    [Fact]
    public async Task EmitirBoletaAsync_ShouldIssueWithTheActiveSeries_AndSplitIgvFromTheTotal()
    {
        var serie = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B003", 0, SunatSerieFiscal.EstadoActiva).Value!;
        SetupMocks([serie]);
        _ = _dbContextMock.Setup(c => c.IncrementarCorrelativoSunatAsync(serie.Id, It.IsAny<CancellationToken>())).ReturnsAsync(7);

        var result = await VentaComprobanteGenerator.EmitirBoletaAsync(_dbContextMock.Object, NuevaVenta(), _sucursalId, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Numero.Should().Be("B003-00000007");
        var comprobante = _comprobantes.Should().ContainSingle().Subject;
        _ = comprobante.SerieId.Should().Be(serie.Id);
        _ = comprobante.MontoTotalVenta.Should().Be(100m);
        _ = comprobante.MontoGravadoIgv.Should().Be(84.75m);
        _ = comprobante.MontoTotalIgv.Should().Be(15.25m);
    }
}
