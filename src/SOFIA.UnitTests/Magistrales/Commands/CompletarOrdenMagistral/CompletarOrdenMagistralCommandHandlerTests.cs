using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Magistrales.Commands.CompletarOrdenMagistral;
using SOFIA.Domain.Entities;
using Xunit;

namespace SOFIA.UnitTests.Magistrales.Commands.CompletarOrdenMagistral;

public class CompletarOrdenMagistralCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly CompletarOrdenMagistralCommandHandler _handler;

    public CompletarOrdenMagistralCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new CompletarOrdenMagistralCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_HappyPath_ShouldCompleteOrderAndCreateInventory()
    {
        // Arrange
        var orden = MagistralOrdenProduccion.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            50m, // CantidadProducida
            Guid.NewGuid(),
            "Iniciada",
            DateTime.UtcNow
        ).Value;

        var command = new CompletarOrdenMagistralCommand(
            orden!.Id,
            "LOTE-MFR-001",
            DateTimeOffset.UtcNow.AddMonths(6)
        );

        var ordenes = new List<MagistralOrdenProduccion> { orden }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.MagistralesOrdenesProduccion).Returns(ordenes.Object);
        _dbContextMock.Setup(db => db.LotesInventario).Returns(new List<LoteInventario>().BuildMockDbSet().Object);
        _dbContextMock.Setup(db => db.LotesEnSucursal).Returns(new List<InventarioSucursal>().BuildMockDbSet().Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().Be(orden.Id);

        orden.EstadoProduccion.Should().Be("Completada");

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(db => db.LotesInventario.Add(It.IsAny<LoteInventario>()), Times.Once);
        _dbContextMock.Verify(db => db.LotesEnSucursal.Add(It.IsAny<InventarioSucursal>()), Times.Once);
        _dbContextMock.Verify(db => db.MagistralesOrdenesProduccion.Update(It.IsAny<MagistralOrdenProduccion>()), Times.Once);
    }

    [Fact]
    public async Task Handle_OrderNotFound_ShouldReturnFailure()
    {
        // Arrange
        var command = new CompletarOrdenMagistralCommand(
            Guid.NewGuid(),
            "LOTE-MFR-001",
            DateTimeOffset.UtcNow.AddMonths(6)
        );

        var ordenes = new List<MagistralOrdenProduccion>().BuildMockDbSet();
        _dbContextMock.Setup(db => db.MagistralesOrdenesProduccion).Returns(ordenes.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orden.NotFound");

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_OrderAlreadyCompleted_ShouldReturnFailure()
    {
        // Arrange
        var orden = MagistralOrdenProduccion.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            50m,
            Guid.NewGuid(),
            "Completada", // Already completed
            DateTime.UtcNow
        ).Value;

        var command = new CompletarOrdenMagistralCommand(
            orden!.Id,
            "LOTE-MFR-001",
            DateTimeOffset.UtcNow.AddMonths(6)
        );

        var ordenes = new List<MagistralOrdenProduccion> { orden }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.MagistralesOrdenesProduccion).Returns(ordenes.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orden.Estado");

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_InvalidCantidadProducida_ShouldReturnFailure()
    {
        // Arrange
        var orden = MagistralOrdenProduccion.Create(
            Guid.NewGuid(),
            Guid.NewGuid(),
            Guid.NewGuid(),
            null,
            null, // Invalid quantity (it's validated out during create, but can simulate state if null bypassed validation somehow)
            Guid.NewGuid(),
            "Iniciada",
            DateTime.UtcNow
        ).Value;

        var command = new CompletarOrdenMagistralCommand(
            orden!.Id,
            "LOTE-MFR-001",
            DateTimeOffset.UtcNow.AddMonths(6)
        );

        var ordenes = new List<MagistralOrdenProduccion> { orden }.BuildMockDbSet();
        _dbContextMock.Setup(db => db.MagistralesOrdenesProduccion).Returns(ordenes.Object);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Code.Should().Be("Orden.Cantidad");

        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
