using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Commands.CreateVenta;

public class CreateVentaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateVentaCommandHandler _handler;
    private readonly List<Venta> _ventasList = [];
    private readonly List<VentaReclamoSeguro> _reclamosList = [];

    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _empleadoId = Guid.NewGuid();
    private readonly Guid _sesionId = Guid.NewGuid();
    private readonly Guid _loteId = Guid.NewGuid();
    private readonly Guid _clienteId = Guid.NewGuid();
    private readonly Guid _productoId = Guid.NewGuid();

    public CreateVentaCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();

        _ = _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(c => c.SucursalId).Returns(_sucursalId.ToString());
        _ = _currentUserMock.Setup(c => c.Id).Returns(_empleadoId.ToString());

        _handler = new CreateVentaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserIsNotAuthenticated()
    {
        // Arrange
        _ = _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);
        var command = new CreateVentaCommand(_clienteId, _sesionId, [], []);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Auth.Sucursal");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenSesionCajaIsNull()
    {
        // Arrange
        var command = new CreateVentaCommand(_clienteId, null, [], []);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Caja");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenSesionCajaDoesNotExist()
    {
        // Arrange
        SetupMocks([]);

        var command = new CreateVentaCommand(_clienteId, _sesionId, [], []);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Caja");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenSesionCajaIsClosed()
    {
        // Arrange
        var sesion = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow.AddHours(-1), 100).Value!;
        sesion.SetId(_sesionId);
        _ = sesion.Cerrar(DateTime.UtcNow, 100, 100);
        SetupMocks(sesionesCaja: [sesion]);

        var command = new CreateVentaCommand(_clienteId, _sesionId, [new CreateVentaDetailDto(_loteId, 1)], [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Caja");
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenSesionCajaBelongsToAnotherCashier()
    {
        // Arrange
        var sesionAjena = PosSesionCaja.Create(_sucursalId, Guid.NewGuid(), DateTime.UtcNow, 100).Value!;
        sesionAjena.SetId(_sesionId);
        var inventario = new List<InventarioSucursal> { InventarioSucursal.Create(_sucursalId, _loteId, 20).Value! };
        SetupMocks(sesionesCaja: [sesionAjena], inventario: inventario);

        var command = new CreateVentaCommand(_clienteId, _sesionId, [new CreateVentaDetailDto(_loteId, 1)], [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Caja.NoPropia");
        _ = result.StatusCode.Should().Be(403);
        _ = _ventasList.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenSesionCajaBelongsToAnotherBranch()
    {
        // Arrange
        var sesionOtraSucursal = PosSesionCaja.Create(Guid.NewGuid(), _empleadoId, DateTime.UtcNow, 100).Value!;
        sesionOtraSucursal.SetId(_sesionId);
        var inventario = new List<InventarioSucursal> { InventarioSucursal.Create(_sucursalId, _loteId, 20).Value! };
        SetupMocks(sesionesCaja: [sesionOtraSucursal], inventario: inventario);

        var command = new CreateVentaCommand(_clienteId, _sesionId, [new CreateVentaDetailDto(_loteId, 1)], [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Caja.NoPropia");
        _ = _ventasList.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenLoteIsInCuarentena()
    {
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        var cuarentenaResult = DigemidInventarioCuarentena.Create(_sucursalId, _loteId, null, 10, "Observación", "Retenido", _empleadoId);
        var cuarentenas = new List<DigemidInventarioCuarentena> { cuarentenaResult.Value! };

        SetupMocks(sesionesCaja: sesiones, cuarentenas: cuarentenas);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            new CreateVentaDetailDto(_loteId, 1)
        ],
        [
            new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Cuarentena");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenLoteDoesNotExistInSucursal()
    {
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        SetupMocks(sesionesCaja: sesiones, inventario: []); // Empty inventory

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            new CreateVentaDetailDto(_loteId, 1)
        ],
        [
            new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Lote");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenStockIsInsufficient()
    {
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        var inventarioItemResult = InventarioSucursal.Create(_sucursalId, _loteId, 5); // Only 5 in stock
        var inventarioItem = inventarioItemResult.Value!;
        var inventario = new List<InventarioSucursal> { inventarioItem! };

        SetupMocks(sesionesCaja: sesiones, inventario: inventario);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            new CreateVentaDetailDto(_loteId, 10) // Requesting 10
        ],
        [
            new CreateVentaPagoDto(MetodoPago.Efectivo, 100, null)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Stock");
    }

    [Fact]
    public async Task Handle_ShouldCreateVenta_Successfully()
    {
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        var inventarioItemResult = InventarioSucursal.Create(_sucursalId, _loteId, 20);
        var inventarioItem = inventarioItemResult.Value!;
        var inventario = new List<InventarioSucursal> { inventarioItem! };

        SetupMocks(sesionesCaja: sesiones, inventario: inventario);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            new CreateVentaDetailDto(_loteId, 2)
        ],
        [
            new CreateVentaPagoDto(MetodoPago.Efectivo, 12, null),
            new CreateVentaPagoDto(MetodoPago.Tarjeta, 8, "AUTH-001")
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().NotBeNull();
        _ = result.Value.VentaId.Should().NotBeEmpty();

        // Verify SaveChanges was called
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);

        // Verify inventory stock was reduced
        _ = inventarioItem.CantidadFisica.Should().Be(18);
    }

    [Fact]
    public async Task Handle_ShouldRejectPaidSale_WithoutConsumingCorrelative_WhenBranchHasNoActiveBoletaSeries()
    {
        // Arrange
        var inventario = new List<InventarioSucursal> { InventarioSucursal.Create(_sucursalId, _loteId, 20).Value! };
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: inventario, series: []);

        var command = new CreateVentaCommand(_clienteId, _sesionId, [new CreateVentaDetailDto(_loteId, 1)], [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.SerieBoleta.NoConfigurada");
        _dbContextMock.Verify(c => c.IncrementarCorrelativoSunatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateVentaPendiente_WithoutComprobante_WhenNoPagosProvided()
    {
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        var inventarioItemResult = InventarioSucursal.Create(_sucursalId, _loteId, 20);
        var inventarioItem = inventarioItemResult.Value!;
        var inventario = new List<InventarioSucursal> { inventarioItem! };

        SetupMocks(sesionesCaja: sesiones, inventario: inventario);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            new CreateVentaDetailDto(_loteId, 2)
        ],
        [],
        Estado: EstadoVenta.Pendiente);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Comprobante.Should().BeNull();

        // Stock is reserved immediately, even though the sale hasn't been paid yet
        _ = inventarioItem.CantidadFisica.Should().Be(18);

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenPagosAreInsufficient()
    {
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        var inventarioItemResult = InventarioSucursal.Create(_sucursalId, _loteId, 20);
        var inventario = new List<InventarioSucursal> { inventarioItemResult.Value! };

        SetupMocks(sesionesCaja: sesiones, inventario: inventario);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            new CreateVentaDetailDto(_loteId, 2) // Total = 20
        ],
        [
            new CreateVentaPagoDto(MetodoPago.Efectivo, 15, null) // Only 15 paid
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Pagos");
    }

    [Fact]
    public async Task Handle_ShouldCreateVenta_WithPresentacion_WhenValid()
    {
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        var inventarioItemResult = InventarioSucursal.Create(_sucursalId, _loteId, 30);
        var inventarioItem = inventarioItemResult.Value!;
        var inventario = new List<InventarioSucursal> { inventarioItem };

        var presentacion = PresentacionVenta.Create(_productoId, Guid.NewGuid(), "Caja x10", 10m, 45m).Value!;

        SetupMocks(sesionesCaja: sesiones, inventario: inventario, presentaciones: [presentacion]);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            // Server price is per base unit (45 per Caja x10 = 4.5 per unidad); 2 Cajas = 20 unidades base, total 90
            new CreateVentaDetailDto(_loteId, 2, null, presentacion.Id)
        ],
        [
            new CreateVentaPagoDto(MetodoPago.Efectivo, 90, null)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = inventarioItem.CantidadFisica.Should().Be(10); // 30 - (2 * 10)

        var detalle = _ventasList[0].Detalles.Single();
        _ = detalle.CantidadVendida.Should().Be(20);
        _ = detalle.PresentacionVentaId.Should().Be(presentacion.Id);
        _ = detalle.CantidadEnPresentacion.Should().Be(2);
        _ = detalle.PrecioFijadoUnidad.Should().Be(4.5m);
        _ = _ventasList[0].MontoTotalBruto.Should().Be(90);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenPresentacionDoesNotExist()
    {
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        var inventarioItemResult = InventarioSucursal.Create(_sucursalId, _loteId, 30);
        var inventario = new List<InventarioSucursal> { inventarioItemResult.Value! };

        SetupMocks(sesionesCaja: sesiones, inventario: inventario, presentaciones: []);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            new CreateVentaDetailDto(_loteId, 2, null, Guid.NewGuid())
        ],
        [
            new CreateVentaPagoDto(MetodoPago.Efectivo, 90, null)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Presentacion");
    }

    [Fact]
    public async Task Handle_ShouldCreateVenta_WithoutPresentacion_WhenNotProvided()
    {
        // Regression: a sale without a presentación keeps behaving exactly as before this feature.
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        var inventarioItemResult = InventarioSucursal.Create(_sucursalId, _loteId, 20);
        var inventarioItem = inventarioItemResult.Value!;
        var inventario = new List<InventarioSucursal> { inventarioItem };

        SetupMocks(sesionesCaja: sesiones, inventario: inventario);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            new CreateVentaDetailDto(_loteId, 2)
        ],
        [
            new CreateVentaPagoDto(MetodoPago.Efectivo, 20, null)
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = inventarioItem.CantidadFisica.Should().Be(18);

        var detalle = _ventasList[0].Detalles.Single();
        _ = detalle.CantidadVendida.Should().Be(2);
        _ = detalle.PrecioFijadoUnidad.Should().Be(10);
        _ = detalle.PresentacionVentaId.Should().BeNull();
        _ = detalle.CantidadEnPresentacion.Should().BeNull();
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenChangeIsGivenWithoutCashPayment()
    {
        // Arrange
        var sesionCajaResult = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100);
        var sesiones = new List<PosSesionCaja> { sesionCajaResult.Value! };
        sesiones[0].SetId(_sesionId);

        var inventarioItemResult = InventarioSucursal.Create(_sucursalId, _loteId, 20);
        var inventario = new List<InventarioSucursal> { inventarioItemResult.Value! };

        SetupMocks(sesionesCaja: sesiones, inventario: inventario);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
        [
            new CreateVentaDetailDto(_loteId, 2) // Total = 20
        ],
        [
            new CreateVentaPagoDto(MetodoPago.Tarjeta, 25, "AUTH-001") // Overpaid by card, no cash line
        ]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Pagos");
    }

    [Fact]
    public async Task Handle_ShouldChargeCatalogPrice_WhenSellingBaseUnits()
    {
        // Arrange
        var inventario = new List<InventarioSucursal> { InventarioSucursal.Create(_sucursalId, _loteId, 20).Value! };
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: inventario, precioCatalogo: 12.5m);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
            [new CreateVentaDetailDto(_loteId, 2)],
            [new CreateVentaPagoDto(MetodoPago.Efectivo, 25, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = _ventasList[0].Detalles.Single().PrecioFijadoUnidad.Should().Be(12.5m);
        _ = _ventasList[0].MontoTotalBruto.Should().Be(25);
    }

    [Fact]
    public async Task Handle_ShouldReturnSinPrecio_WhenProductHasNoCatalogPrice()
    {
        // Arrange
        var inventarioItem = InventarioSucursal.Create(_sucursalId, _loteId, 20).Value!;
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: [inventarioItem], precioCatalogo: null);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
            [new CreateVentaDetailDto(_loteId, 2)],
            [new CreateVentaPagoDto(MetodoPago.Efectivo, 20, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.SinPrecio");
        _ = inventarioItem.CantidadFisica.Should().Be(20);
    }

    [Fact]
    public async Task Handle_ShouldSnapshotCurrentSupplierCost_WhenCreatingDetalle()
    {
        // Arrange
        var inventario = new List<InventarioSucursal> { InventarioSucursal.Create(_sucursalId, _loteId, 20).Value! };
        var vencido = HistorialPrecioProveedor.Create(Guid.NewGuid(), _productoId, 4, DateTime.UtcNow.AddMonths(-6), DateTime.UtcNow.AddMonths(-1), 3).Value!;
        var vigente = HistorialPrecioProveedor.Create(Guid.NewGuid(), _productoId, 6, DateTime.UtcNow.AddMonths(-1), null, 3).Value!;
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: inventario, historialPrecios: [vencido, vigente]);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
            [new CreateVentaDetailDto(_loteId, 2)],
            [new CreateVentaPagoDto(MetodoPago.Efectivo, 20, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = _ventasList[0].Detalles.Single().CostoUnitarioHistorico.Should().Be(6);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenPresentacionBelongsToAnotherProduct()
    {
        // Arrange
        var inventario = new List<InventarioSucursal> { InventarioSucursal.Create(_sucursalId, _loteId, 30).Value! };
        var presentacionAjena = PresentacionVenta.Create(Guid.NewGuid(), Guid.NewGuid(), "Caja x10", 10m, 1m).Value!;
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: inventario, presentaciones: [presentacionAjena]);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
            [new CreateVentaDetailDto(_loteId, 1, null, presentacionAjena.Id)],
            [new CreateVentaPagoDto(MetodoPago.Efectivo, 1, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Presentacion");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAseguradoraDoesNotExist()
    {
        // Arrange
        var inventarioItem = InventarioSucursal.Create(_sucursalId, _loteId, 20).Value!;
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: [inventarioItem], aseguradoras: []);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
            [new CreateVentaDetailDto(_loteId, 2)],
            [new CreateVentaPagoDto(MetodoPago.Efectivo, 5, null)],
            AseguradoraId: Guid.NewGuid(),
            MontoCubiertoSeguro: 15);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Aseguradora.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreCoverage_WhenNoAseguradoraIsProvided()
    {
        // Arrange
        var inventario = new List<InventarioSucursal> { InventarioSucursal.Create(_sucursalId, _loteId, 20).Value! };
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: inventario);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
            [new CreateVentaDetailDto(_loteId, 2)], // Total = 20
            [new CreateVentaPagoDto(MetodoPago.Efectivo, 0.01m, null)],
            MontoCubiertoSeguro: 19.99m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Pagos");
        _ = _reclamosList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCoverageExceedsTheSaleTotal()
    {
        // Arrange
        var aseguradora = AseguradoraMedica.Create("Rimac", "RIMAC-01").Value!;
        var inventario = new List<InventarioSucursal> { InventarioSucursal.Create(_sucursalId, _loteId, 20).Value! };
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: inventario, aseguradoras: [aseguradora]);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
            [new CreateVentaDetailDto(_loteId, 2)], // Total = 20
            [new CreateVentaPagoDto(MetodoPago.Efectivo, 0.01m, null)],
            AseguradoraId: aseguradora.Id,
            MontoCubiertoSeguro: 25);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Seguro.MontoInvalido");
        _ = _reclamosList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateClaim_WhenCoverageHasAValidAseguradora()
    {
        // Arrange
        var aseguradora = AseguradoraMedica.Create("Rimac", "RIMAC-01").Value!;
        var inventario = new List<InventarioSucursal> { InventarioSucursal.Create(_sucursalId, _loteId, 20).Value! };
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: inventario, aseguradoras: [aseguradora]);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
            [new CreateVentaDetailDto(_loteId, 2)], // Total = 20
            [new CreateVentaPagoDto(MetodoPago.Efectivo, 5, null)],
            AseguradoraId: aseguradora.Id,
            MontoCubiertoSeguro: 15);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        var reclamo = _reclamosList.Should().ContainSingle().Subject;
        _ = reclamo.AseguradoraId.Should().Be(aseguradora.Id);
        _ = reclamo.MontoCubierto.Should().Be(15);
        _ = reclamo.MontoCopagoPaciente.Should().Be(5);
    }

    [Fact]
    public async Task Handle_ShouldReturnRecetaRequerida_WhenControlledProductIsSoldWithoutPrescription()
    {
        // Arrange
        var inventarioItem = InventarioSucursal.Create(_sucursalId, _loteId, 20).Value!;
        SetupMocks(sesionesCaja: SesionAbierta(), inventario: [inventarioItem], condicionVenta: CondicionVenta.Estupefaciente);

        var command = new CreateVentaCommand(_clienteId, _sesionId,
            [new CreateVentaDetailDto(_loteId, 1)],
            [new CreateVentaPagoDto(MetodoPago.Efectivo, 10, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Receta.Requerida");
        _ = _ventasList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private List<PosSesionCaja> SesionAbierta()
    {
        var sesion = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow, 100).Value!;
        sesion.SetId(_sesionId);
        return [sesion];
    }

    private void SetupMocks(
        List<PosSesionCaja>? sesionesCaja = null,
        List<DigemidInventarioCuarentena>? cuarentenas = null,
        List<InventarioSucursal>? inventario = null,
        List<SunatSerieFiscal>? series = null,
        List<PresentacionVenta>? presentaciones = null,
        decimal? precioCatalogo = 10,
        List<HistorialPrecioProveedor>? historialPrecios = null,
        List<AseguradoraMedica>? aseguradoras = null,
        CondicionVenta condicionVenta = CondicionVenta.VentaLibreOTC)
    {
        sesionesCaja ??= [];
        cuarentenas ??= [];
        inventario ??= [];
        series ??= [SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B001", 0, SunatSerieFiscal.EstadoActiva).Value!];
        presentaciones ??= [];
        historialPrecios ??= [];
        aseguradoras ??= [];

        var lote = LoteInventario.Create(_productoId, "L-001", null, DateTimeOffset.UtcNow.AddYears(1)).Value!;
        lote.SetId(_loteId);
        var medicamento = Medicamento.Create("COD-1", "Producto", Guid.NewGuid(), Guid.NewGuid(), condicionVenta, precioCatalogo).Value!;
        medicamento.SetId(_productoId);
        _ = _dbContextMock.Setup(c => c.LotesInventario).Returns(new List<LoteInventario> { lote }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { medicamento }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.HistorialPreciosProveedor).Returns(historialPrecios.BuildMockDbSet().Object);

        _ = _dbContextMock.Setup(c => c.POSSesionesCaja).Returns(sesionesCaja.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.DigemidInventarioCuarentena).Returns(cuarentenas.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.LotesEnSucursal).Returns(inventario.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SUNATSeriesFiscales).Returns(series.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.IncrementarCorrelativoSunatAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _dbContextMock.Setup(c => c.PresentacionesVenta).Returns(presentaciones.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Aseguradoras).Returns(aseguradoras.BuildMockDbSet().Object);

        // Setup Add for Ventas
        var ventasDbSetMock = _ventasList.BuildMockDbSet();
        _ = ventasDbSetMock.Setup(d => d.Add(It.IsAny<Venta>())).Callback<Venta>(_ventasList.Add);
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(ventasDbSetMock.Object);

        // Setup Add for Comprobantes
        var comprobantesList = new List<SunatComprobanteEmitido>();
        var comprobantesDbSetMock = comprobantesList.BuildMockDbSet();
        _ = comprobantesDbSetMock.Setup(d => d.Add(It.IsAny<SunatComprobanteEmitido>())).Callback<SunatComprobanteEmitido>(comprobantesList.Add);
        _ = _dbContextMock.Setup(c => c.SUNATComprobantesEmitidos).Returns(comprobantesDbSetMock.Object);

        // Setup Add for Reclamos
        var reclamosDbSetMock = _reclamosList.BuildMockDbSet();
        _ = reclamosDbSetMock.Setup(d => d.Add(It.IsAny<VentaReclamoSeguro>())).Callback<VentaReclamoSeguro>(_reclamosList.Add);
        _ = _dbContextMock.Setup(c => c.VentasReclamosSeguro).Returns(reclamosDbSetMock.Object);

        // Setup Add for Outbox
        var outboxList = new List<SistemaOutboxEvento>();
        var outboxDbSetMock = outboxList.BuildMockDbSet();
        _ = outboxDbSetMock.Setup(d => d.Add(It.IsAny<SistemaOutboxEvento>())).Callback<SistemaOutboxEvento>(outboxList.Add);
        _ = _dbContextMock.Setup(c => c.SistemaOutboxEventos).Returns(outboxDbSetMock.Object);

    }
}
