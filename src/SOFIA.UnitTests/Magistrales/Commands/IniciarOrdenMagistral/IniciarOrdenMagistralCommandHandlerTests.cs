using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Magistrales.Commands.IniciarOrdenMagistral;

public class IniciarOrdenMagistralCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly IniciarOrdenMagistralCommandHandler _handler;

    public IniciarOrdenMagistralCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new IniciarOrdenMagistralCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldCreateOrderAndConsumeStock()
    {
        // Arrange
        var inventario = InventarioSucursal.Create(Guid.NewGuid(), Guid.NewGuid(), 50m).Value;
        var command = new IniciarOrdenMagistralCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            [
                new(inventario!.Id, 10)
            ]
        );

        var lotesEnSucursal = new List<InventarioSucursal> { inventario }.BuildMockDbSet();
        _ = _dbContextMock.Setup(db => db.LotesEnSucursal).Returns(lotesEnSucursal.Object);
        _ = _dbContextMock.Setup(db => db.MagistralesConsumosInsumo).Returns(new List<MagistralConsumoInsumo>().BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(db => db.MagistralesOrdenesProduccion).Returns(new List<MagistralOrdenProduccion>().BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().NotBeEmpty();

        // Check if stock was reduced
        _ = inventario.CantidadFisica.Should().Be(40m);

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(db => db.MagistralesOrdenesProduccion.Add(It.IsAny<MagistralOrdenProduccion>()), Times.Once);
        _dbContextMock.Verify(db => db.MagistralesConsumosInsumo.Add(It.IsAny<MagistralConsumoInsumo>()), Times.Once);
    }

    [Fact]
    public async Task Handle_InsufficientStock_ShouldReturnFailure()
    {
        // Arrange
        var inventario = InventarioSucursal.Create(Guid.NewGuid(), Guid.NewGuid(), 5m).Value;
        var command = new IniciarOrdenMagistralCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            [
                new(inventario!.Id, 10) // Tries to consume 10, but stock is 5
            ]
        );

        var lotesEnSucursal = new List<InventarioSucursal> { inventario }.BuildMockDbSet();
        _ = _dbContextMock.Setup(db => db.LotesEnSucursal).Returns(lotesEnSucursal.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Inventario.StockInsuficiente");
        _ = inventario.CantidadFisica.Should().Be(5m); // Stock should not change

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InventoryNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new IniciarOrdenMagistralCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            10,
            [
                new(Guid.NewGuid(), 10)
            ]
        );

        var lotesEnSucursal = new List<InventarioSucursal>().BuildMockDbSet();
        _ = _dbContextMock.Setup(db => db.LotesEnSucursal).Returns(lotesEnSucursal.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Inventario.NotFound");

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
