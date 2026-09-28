using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Inventarios.Commands.AdjustStock;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Inventarios.Commands.AdjustStock;

public class AdjustStockCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ISucursalAccess> _sucursalAccessMock = new();
    private Mock<Microsoft.EntityFrameworkCore.DbSet<InventarioSucursal>> _stockMock = new List<InventarioSucursal>().BuildMockDbSet();
    private readonly AdjustStockCommandHandler _handler;

    public AdjustStockCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.LotesEnSucursal).Returns(() => _stockMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _sucursalAccessMock.Setup(a => a.CanAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _handler = new AdjustStockCommandHandler(_dbContextMock.Object, _sucursalAccessMock.Object);
    }

    private static InventarioSucursal NewEntry(decimal cantidad = 50) =>
        InventarioSucursal.Create(Guid.NewGuid(), Guid.NewGuid(), cantidad).Value!;

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenRecordDoesNotExist()
    {
        var result = await _handler.Handle(new AdjustStockCommand(Guid.NewGuid(), 10), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("InventarioSucursal.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenQuantityIsNegative()
    {
        var entry = NewEntry(50);
        _stockMock = new List<InventarioSucursal> { entry }.BuildMockDbSet();

        var result = await _handler.Handle(new AdjustStockCommand(entry.Id, -1), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("InventarioSucursal.CantidadFisica");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndSave_WhenQuantityIsValid()
    {
        var entry = NewEntry(50);
        _stockMock = new List<InventarioSucursal> { entry }.BuildMockDbSet();

        var result = await _handler.Handle(new AdjustStockCommand(entry.Id, 12), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = entry.CantidadFisica.Should().Be(12);
        _sucursalAccessMock.Verify(a => a.CanAccessAsync(entry.SucursalId, It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenBranchIsNotAllowed()
    {
        var entry = NewEntry(50);
        _stockMock = new List<InventarioSucursal> { entry }.BuildMockDbSet();
        _ = _sucursalAccessMock.Setup(a => a.CanAccessAsync(entry.SucursalId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _handler.Handle(new AdjustStockCommand(entry.Id, 12), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Inventario.Sucursal.NoPermitida");
        _ = result.StatusCode.Should().Be(403);
        _ = entry.CantidadFisica.Should().Be(50);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
