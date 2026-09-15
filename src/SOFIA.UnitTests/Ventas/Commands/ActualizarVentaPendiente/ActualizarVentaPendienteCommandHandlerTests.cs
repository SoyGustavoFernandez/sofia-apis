using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.ActualizarVentaPendiente;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Commands.ActualizarVentaPendiente;

public class ActualizarVentaPendienteCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly ActualizarVentaPendienteCommandHandler _handler;

    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _empleadoId = Guid.NewGuid();
    private readonly Guid _loteOriginalId = Guid.NewGuid();
    private readonly Guid _loteNuevoId = Guid.NewGuid();

    public ActualizarVentaPendienteCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();

        _ = _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(c => c.SucursalId).Returns(_sucursalId.ToString());
        _ = _currentUserMock.Setup(c => c.Id).Returns(_empleadoId.ToString());

        _handler = new ActualizarVentaPendienteCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    private Venta CrearVentaPendiente(Guid? sucursalId = null)
    {
        var detalle = DetalleVenta.Create(_loteOriginalId, 2, 10, 5).Value!;
        return Venta.Create(sucursalId ?? _sucursalId, _empleadoId, null, Guid.NewGuid(), [detalle], EstadoVenta.Pendiente).Value!;
    }

    private void SetupMocks(List<Venta> ventas, List<InventarioSucursal> inventario, List<DigemidInventarioCuarentena>? cuarentenas = null)
    {
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(ventas.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.LotesEnSucursal).Returns(inventario.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.DigemidInventarioCuarentena).Returns((cuarentenas ?? []).BuildMockDbSet().Object);

        var detallesList = new List<DetalleVenta>();
        var detallesDbSetMock = detallesList.BuildMockDbSet();
        _ = detallesDbSetMock.Setup(d => d.Add(It.IsAny<DetalleVenta>())).Callback<DetalleVenta>(detallesList.Add);
        _ = detallesDbSetMock.Setup(d => d.RemoveRange(It.IsAny<IEnumerable<DetalleVenta>>()));
        _ = _dbContextMock.Setup(c => c.DetallesVenta).Returns(detallesDbSetMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenVentaDoesNotExist()
    {
        // Arrange
        SetupMocks([], []);
        var command = new ActualizarVentaPendienteCommand(Guid.NewGuid(), [new CreateVentaDetailDto(_loteNuevoId, 1, 10, 5)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Actualizar");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenVentaIsNotPendiente()
    {
        // Arrange
        var detalle = DetalleVenta.Create(_loteOriginalId, 2, 10, 5).Value!;
        var venta = Venta.Create(_sucursalId, _empleadoId, null, Guid.NewGuid(), [detalle]).Value!; // Completada
        SetupMocks([venta], []);
        var command = new ActualizarVentaPendienteCommand(venta.Id, [new CreateVentaDetailDto(_loteNuevoId, 1, 10, 5)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Actualizar");
    }

    [Fact]
    public async Task Handle_ShouldReleaseOldStockAndReserveNewStock_WhenReplacingDetalles()
    {
        // Arrange
        var venta = CrearVentaPendiente();
        var inventarioOriginal = InventarioSucursal.Create(_sucursalId, _loteOriginalId, 8).Value!; // 8 left after reserving 2 of 10
        var inventarioNuevo = InventarioSucursal.Create(_sucursalId, _loteNuevoId, 20).Value!;
        SetupMocks([venta], [inventarioOriginal, inventarioNuevo]);

        var command = new ActualizarVentaPendienteCommand(venta.Id, [new CreateVentaDetailDto(_loteNuevoId, 4, 6, 3)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = inventarioOriginal.CantidadFisica.Should().Be(10); // fully released
        _ = inventarioNuevo.CantidadFisica.Should().Be(16); // 20 - 4 reserved
        _ = venta.Detalles.Should().ContainSingle(d => d.LoteId == _loteNuevoId && d.CantidadVendida == 4);
        _ = venta.MontoTotalBruto.Should().Be(24); // 4 x 6

        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenNewLoteHasInsufficientStock()
    {
        // Arrange
        var venta = CrearVentaPendiente();
        var inventarioOriginal = InventarioSucursal.Create(_sucursalId, _loteOriginalId, 8).Value!;
        var inventarioNuevo = InventarioSucursal.Create(_sucursalId, _loteNuevoId, 2).Value!; // Only 2 available
        SetupMocks([venta], [inventarioOriginal, inventarioNuevo]);

        var command = new ActualizarVentaPendienteCommand(venta.Id, [new CreateVentaDetailDto(_loteNuevoId, 4, 6, 3)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Stock");
    }
}
