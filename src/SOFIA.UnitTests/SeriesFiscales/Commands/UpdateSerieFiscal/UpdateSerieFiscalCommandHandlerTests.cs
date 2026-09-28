using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.SeriesFiscales.Commands.UpdateSerieFiscal;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.SeriesFiscales.Commands.UpdateSerieFiscal;

public class UpdateSerieFiscalCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Sucursal _sucursal = Sucursal.Create("Central", "Av. Lima 123", "LIC-001").Value!;
    private readonly SunatSerieFiscal _serie;
    private readonly List<SunatSerieFiscal> _series = [];
    private readonly List<SunatComprobanteEmitido> _comprobantes = [];

    public UpdateSerieFiscalCommandHandlerTests()
    {
        _serie = SunatSerieFiscal.Create(_sucursal.Id, TipoComprobante.Boleta, "B001", 10, SunatSerieFiscal.EstadoInactiva).Value!;
        _series.Add(_serie);
    }

    private UpdateSerieFiscalCommandHandler CreateHandler()
    {
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(new List<Sucursal> { _sucursal }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SUNATSeriesFiscales).Returns(_series.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SUNATComprobantesEmitidos).Returns(_comprobantes.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return new UpdateSerieFiscalCommandHandler(_dbContextMock.Object);
    }

    private UpdateSerieFiscalCommand Command(string prefijo = "B001", int correlativo = 10, string estado = SunatSerieFiscal.EstadoActiva) =>
        new(_serie.Id, _sucursal.Id, TipoComprobante.Boleta, prefijo, correlativo, estado);

    private void EmitirComprobante() =>
        _comprobantes.Add(SunatComprobanteEmitido.Create(
            Guid.NewGuid(), _serie.Id, 10, "1", "00000000", "CLIENTE EVENTUAL", 8.2m, 0, 1.8m, 10m, null, "Aceptado", null, null, null).Value!);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenSeriesDoesNotExist()
    {
        var result = await CreateHandler().Handle(Command() with { Id = Guid.NewGuid() }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldUpdateSeries_WhenNoConflicts()
    {
        var result = await CreateHandler().Handle(Command(prefijo: "B005", correlativo: 0), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _serie.PrefijoSerie.Should().Be("B005");
        _ = _serie.CorrelativoActual.Should().Be(0);
        _ = _serie.EsActiva.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenCorrelativeChangesAfterIssuingDocuments()
    {
        EmitirComprobante();

        var result = await CreateHandler().Handle(Command(correlativo: 3), CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.ConComprobantes");
        _ = result.StatusCode.Should().Be(409);
        _ = _serie.CorrelativoActual.Should().Be(10);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenPrefixChangesAfterIssuingDocuments()
    {
        EmitirComprobante();

        var result = await CreateHandler().Handle(Command(prefijo: "B009"), CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.ConComprobantes");
    }

    [Fact]
    public async Task Handle_ShouldAllowStatusChange_AfterIssuingDocuments()
    {
        EmitirComprobante();

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _serie.EsActiva.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenActivatingAndAnotherActiveSeriesExists()
    {
        _series.Add(SunatSerieFiscal.Create(_sucursal.Id, TipoComprobante.Boleta, "B002", 0, SunatSerieFiscal.EstadoActiva).Value!);

        var result = await CreateHandler().Handle(Command(), CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.ActivaDuplicada");
        _ = result.StatusCode.Should().Be(409);
        _ = _serie.EsActiva.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenNewPrefixIsTakenForTheType()
    {
        _series.Add(SunatSerieFiscal.Create(Guid.NewGuid(), TipoComprobante.Boleta, "B002", 0, SunatSerieFiscal.EstadoInactiva).Value!);

        var result = await CreateHandler().Handle(Command(prefijo: "B002", estado: SunatSerieFiscal.EstadoInactiva), CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.PrefijoDuplicado");
    }
}
