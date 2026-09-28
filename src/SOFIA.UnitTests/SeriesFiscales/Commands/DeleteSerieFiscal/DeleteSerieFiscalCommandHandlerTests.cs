using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.SeriesFiscales.Commands.DeleteSerieFiscal;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.SeriesFiscales.Commands.DeleteSerieFiscal;

public class DeleteSerieFiscalCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly SunatSerieFiscal _serie = SunatSerieFiscal.Create(Guid.NewGuid(), TipoComprobante.Boleta, "B001", 0, SunatSerieFiscal.EstadoActiva).Value!;
    private readonly List<SunatComprobanteEmitido> _comprobantes = [];
    private Mock<Microsoft.EntityFrameworkCore.DbSet<SunatSerieFiscal>> _seriesMock = new List<SunatSerieFiscal>().BuildMockDbSet();

    private DeleteSerieFiscalCommandHandler CreateHandler()
    {
        _seriesMock = new List<SunatSerieFiscal> { _serie }.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.SUNATSeriesFiscales).Returns(_seriesMock.Object);
        _ = _dbContextMock.Setup(c => c.SUNATComprobantesEmitidos).Returns(_comprobantes.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        return new DeleteSerieFiscalCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenSeriesDoesNotExist()
    {
        var result = await CreateHandler().Handle(new DeleteSerieFiscalCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenSeriesIssuedDocuments()
    {
        _comprobantes.Add(SunatComprobanteEmitido.Create(
            Guid.NewGuid(), _serie.Id, 1, "1", "00000000", "CLIENTE EVENTUAL", 8.2m, 0, 1.8m, 10m, null, "Aceptado", null, null, null).Value!);
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteSerieFiscalCommand(_serie.Id), CancellationToken.None);

        _ = result.Error.Code.Should().Be("SerieFiscal.InUse");
        _ = result.StatusCode.Should().Be(409);
        _seriesMock.Verify(m => m.Remove(It.IsAny<SunatSerieFiscal>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldDeleteSeries_WhenUnused()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new DeleteSerieFiscalCommand(_serie.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _seriesMock.Verify(m => m.Remove(_serie), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
