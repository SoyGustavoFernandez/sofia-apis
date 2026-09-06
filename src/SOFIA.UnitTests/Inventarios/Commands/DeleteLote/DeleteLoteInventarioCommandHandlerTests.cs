using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Inventarios.Commands.DeleteLote;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Inventarios.Commands.DeleteLote;

public class DeleteLoteInventarioCommandHandlerTests
{
    private static readonly DateTimeOffset FutureDate = DateTimeOffset.UtcNow.AddYears(1);

    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private Mock<Microsoft.EntityFrameworkCore.DbSet<LoteInventario>> _lotesMock = new List<LoteInventario>().BuildMockDbSet();
    private Mock<Microsoft.EntityFrameworkCore.DbSet<InventarioSucursal>> _stockMock = new List<InventarioSucursal>().BuildMockDbSet();
    private Mock<Microsoft.EntityFrameworkCore.DbSet<DetalleVenta>> _ventasMock = new List<DetalleVenta>().BuildMockDbSet();
    private readonly DeleteLoteInventarioHandler _handler;

    public DeleteLoteInventarioCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.LotesInventario).Returns(() => _lotesMock.Object);
        _ = _dbContextMock.Setup(c => c.LotesEnSucursal).Returns(() => _stockMock.Object);
        _ = _dbContextMock.Setup(c => c.DetallesVenta).Returns(() => _ventasMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new DeleteLoteInventarioHandler(_dbContextMock.Object);
    }

    private static LoteInventario NewLote() =>
        LoteInventario.Create(Guid.NewGuid(), "LOT-001", null, FutureDate).Value!;

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLoteDoesNotExist()
    {
        var result = await _handler.Handle(new DeleteLoteInventarioCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenLoteHasStock()
    {
        var lote = NewLote();
        _lotesMock = new List<LoteInventario> { lote }.BuildMockDbSet();
        _stockMock = new List<InventarioSucursal>
        {
            InventarioSucursal.Create(Guid.NewGuid(), lote.Id, 10).Value!,
        }.BuildMockDbSet();

        var result = await _handler.Handle(new DeleteLoteInventarioCommand(lote.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.InUse");
        _ = result.StatusCode.Should().Be(409);
        _lotesMock.Verify(m => m.Remove(It.IsAny<LoteInventario>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenLoteHasSales()
    {
        var lote = NewLote();
        _lotesMock = new List<LoteInventario> { lote }.BuildMockDbSet();
        _ventasMock = new List<DetalleVenta>
        {
            DetalleVenta.Create(lote.Id, 1, 5, 3).Value!,
        }.BuildMockDbSet();

        var result = await _handler.Handle(new DeleteLoteInventarioCommand(lote.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("LoteInventario.InUse");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenLoteHasNoStockOrSales()
    {
        var lote = NewLote();
        _lotesMock = new List<LoteInventario> { lote }.BuildMockDbSet();

        var result = await _handler.Handle(new DeleteLoteInventarioCommand(lote.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _lotesMock.Verify(m => m.Remove(lote), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
