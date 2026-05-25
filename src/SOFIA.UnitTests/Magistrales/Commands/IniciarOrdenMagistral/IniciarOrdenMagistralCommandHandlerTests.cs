using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;
using SOFIA.Domain.Entities;
using Xunit;

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
            new List<InsumoDto>
            {
                new InsumoDto(inventario!.Id, 10)
            }
        );

        var lotesEnSucursal = new List<InventarioSucursal> { inventario }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.LotesEnSucursal).Returns(lotesEnSucursal.Object);
        _dbContextMock.Setup(db => db.MagistralesConsumosInsumo).Returns(new List<MagistralConsumoInsumo>().BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.MagistralesOrdenesProduccion).Returns(new List<MagistralOrdenProduccion>().BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeEmpty();

        // Check if stock was reduced
        inventario.CantidadFisica.Should().Be(40m);

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
            new List<InsumoDto>
            {
                new InsumoDto(inventario!.Id, 10) // Tries to consume 10, but stock is 5
            }
        );

        var lotesEnSucursal = new List<InventarioSucursal> { inventario }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.LotesEnSucursal).Returns(lotesEnSucursal.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Inventario.StockInsuficiente");
        inventario.CantidadFisica.Should().Be(5m); // Stock should not change

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
            new List<InsumoDto>
            {
                new InsumoDto(Guid.NewGuid(), 10)
            }
        );

        var lotesEnSucursal = new List<InventarioSucursal>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.LotesEnSucursal).Returns(lotesEnSucursal.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Inventario.NotFound");

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
