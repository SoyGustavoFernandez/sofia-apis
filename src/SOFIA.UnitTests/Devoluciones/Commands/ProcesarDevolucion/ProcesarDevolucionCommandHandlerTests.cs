using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Devoluciones.Commands.ProcesarDevolucion;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Devoluciones.Commands.ProcesarDevolucion;

public class ProcesarDevolucionCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly ProcesarDevolucionCommandHandler _handler;

    public ProcesarDevolucionCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _handler = new ProcesarDevolucionCommandHandler(_dbContextMock.Object);
    }

    private void SetupDbContext(
        List<Venta> ventas,
        List<InventarioSucursal> inventarios,
        List<SunatSerieFiscal> series)
    {
        var ventasMock = ventas.BuildMockDbSet();
        _ = _dbContextMock.Setup(x => x.Ventas).Returns(ventasMock.Object);

        var inventariosMock = inventarios.BuildMockDbSet();
        _ = _dbContextMock.Setup(x => x.LotesEnSucursal).Returns(inventariosMock.Object);

        var seriesMock = series.BuildMockDbSet();
        _ = _dbContextMock.Setup(x => x.SUNATSeriesFiscales).Returns(seriesMock.Object);

        var devolucionesMock = new List<DevolucionCabecera>().BuildMockDbSet();
        _ = _dbContextMock.Setup(x => x.Devoluciones).Returns(devolucionesMock.Object);

        var comprobantesMock = new List<SunatComprobanteEmitido>().BuildMockDbSet();
        _ = _dbContextMock.Setup(x => x.SUNATComprobantesEmitidos).Returns(comprobantesMock.Object);
    }

    [Fact]
    public async Task Handle_WhenVentaNotFound_ShouldReturnFailure()
    {
        // Arrange
        SetupDbContext([], [], []);

        var command = new ProcesarDevolucionCommand(
            Guid.NewGuid(),
            Guid.NewGuid(),
            "07",
            "Motivo",
            [new(Guid.NewGuid(), 1m, DestinoDevolucion.Reingreso_Venta)]
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.NotFound");
    }

    [Fact]
    public async Task Handle_WhenDetalleNotFound_ShouldReturnFailure()
    {
        // Arrange
        var sucursalId = Guid.NewGuid();
        var venta = Venta.Create(sucursalId, Guid.NewGuid(), null, null,
        [
            DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!
        ]).Value!;

        SetupDbContext([venta], [], []);

        var command = new ProcesarDevolucionCommand(
            venta.Id,
            Guid.NewGuid(),
            "07",
            "Motivo",
            [new(Guid.NewGuid(), 1m, DestinoDevolucion.Reingreso_Venta)] // Invalid DetalleVentaId
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("DetalleVenta.NotFound");
    }

    [Fact]
    public async Task Handle_WhenCantidadExceedsVendida_ShouldReturnFailure()
    {
        // Arrange
        var sucursalId = Guid.NewGuid();
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = Venta.Create(sucursalId, Guid.NewGuid(), null, null, [detalle]).Value!;

        SetupDbContext([venta], [], []);

        var command = new ProcesarDevolucionCommand(
            venta.Id,
            Guid.NewGuid(),
            "07",
            "Motivo",
            [new(detalle.Id, 6m, DestinoDevolucion.Reingreso_Venta)] // 6 > 5
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Cantidad");
    }

    [Fact]
    public async Task Handle_WhenValidRequest_ShouldProcessAndGenerateNC()
    {
        // Arrange
        var sucursalId = Guid.NewGuid();
        var loteId1 = Guid.NewGuid();
        var loteId2 = Guid.NewGuid();

        var detalle1 = DetalleVenta.Create(loteId1, 5m, 10m, 8m).Value!;
        var detalle2 = DetalleVenta.Create(loteId2, 2m, 20m, 15m).Value!;

        var venta = Venta.Create(sucursalId, Guid.NewGuid(), null, null, [detalle1, detalle2]).Value!;

        var inventario1 = InventarioSucursal.Create(sucursalId, loteId1, 50m).Value!;
        var inventario2 = InventarioSucursal.Create(sucursalId, loteId2, 20m).Value!;

        var serie = SunatSerieFiscal.Create(sucursalId, TipoComprobante.NotaCredito, "FN01", 100, "Activa").Value!;

        SetupDbContext(
            [venta],
            [inventario1, inventario2],
            [serie]
        );
        _ = _dbContextMock.Setup(x => x.IncrementarCorrelativoSunatAsync(serie.Id, It.IsAny<CancellationToken>())).ReturnsAsync(101);

        var command = new ProcesarDevolucionCommand(
            venta.Id,
            Guid.NewGuid(),
            "07",
            "Devolución parcial",
            [
                new(detalle1.Id, 2m, DestinoDevolucion.Reingreso_Venta),
                new(detalle2.Id, 1m, DestinoDevolucion.Cuarentena_DIGEMID)
            ]
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();

        // Assert inventory is updated for Reingreso_Venta only
        _ = inventario1.CantidadFisica.Should().Be(52m); // 50 + 2
        _ = inventario2.CantidadFisica.Should().Be(20m); // Unchanged since it's Cuarentena

        _dbContextMock.Verify(x => x.Devoluciones.Add(It.IsAny<DevolucionCabecera>()), Times.Once);
        _dbContextMock.Verify(x => x.LotesEnSucursal.Update(inventario1), Times.Once);
        _dbContextMock.Verify(x => x.SUNATComprobantesEmitidos.Add(It.Is<SunatComprobanteEmitido>(nc => nc.NumeroCorrelativo == 101)), Times.Once);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoActiveSerie_ShouldProcessWithoutNC()
    {
        // Arrange
        var sucursalId = Guid.NewGuid();
        var loteId = Guid.NewGuid();
        var detalle = DetalleVenta.Create(loteId, 5m, 10m, 8m).Value!;
        var venta = Venta.Create(sucursalId, Guid.NewGuid(), null, null, [detalle]).Value!;

        var inventario = InventarioSucursal.Create(sucursalId, loteId, 50m).Value!;

        SetupDbContext(
            [venta],
            [inventario],
            [] // No series available
        );

        var command = new ProcesarDevolucionCommand(
            venta.Id,
            Guid.NewGuid(),
            "07",
            "Devolución parcial",
            [new(detalle.Id, 2m, DestinoDevolucion.Reingreso_Venta)]
        );

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();

        _dbContextMock.Verify(x => x.Devoluciones.Add(It.IsAny<DevolucionCabecera>()), Times.Once);
        _dbContextMock.Verify(x => x.SUNATSeriesFiscales.Update(It.IsAny<SunatSerieFiscal>()), Times.Never);
        _dbContextMock.Verify(x => x.SUNATComprobantesEmitidos.Add(It.IsAny<SunatComprobanteEmitido>()), Times.Never);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
