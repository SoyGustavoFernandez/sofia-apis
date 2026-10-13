using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Sucursales.Commands.DeleteSucursal;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Sucursales.Commands.DeleteSucursal;

public class DeleteSucursalCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<DbSet<Sucursal>> _sucursalesMock = new();
    private readonly Sucursal _sucursal = Sucursal.Create("Sede", "Av. Uno 123", "LIC-001").Value!;
    private readonly DeleteSucursalCommandHandler _handler;

    public DeleteSucursalCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(_sucursalesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _sucursalesMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Sucursal?>(_sucursal));
        SetupDependents();
        _handler = new DeleteSucursalCommandHandler(_dbContextMock.Object);
    }

    private sealed record Dependents(
        Empleado? Empleado = null,
        Cuenta? Cuenta = null,
        Rol? Rol = null,
        InventarioSucursal? Inventario = null,
        PosSesionCaja? Sesion = null,
        SunatSerieFiscal? Serie = null,
        Venta? Venta = null,
        Transferencia? Transferencia = null,
        DigemidInventarioCuarentena? Cuarentena = null,
        MagistralOrdenProduccion? Orden = null);

    private void SetupDependents(Dependents? d = null)
    {
        d ??= new Dependents();
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(Of(d.Empleado).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(Of(d.Cuenta).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Roles).Returns(Of(d.Rol).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.LotesEnSucursal).Returns(Of(d.Inventario).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.POSSesionesCaja).Returns(Of(d.Sesion).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SUNATSeriesFiscales).Returns(Of(d.Serie).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(Of(d.Venta).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Transferencias).Returns(Of(d.Transferencia).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.DigemidInventarioCuarentena).Returns(Of(d.Cuarentena).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.MagistralesOrdenesProduccion).Returns(Of(d.Orden).BuildMockDbSet().Object);
    }

    private static List<T> Of<T>(T? item) where T : class => item is null ? [] : [item];

    private Empleado NewEmpleado() => Empleado.Create(_sucursal.Id, "Ana", "Perez", "Gomez").Value!;

    private Cuenta NewCuenta()
    {
        var cuenta = Cuenta.Create(Guid.NewGuid(), "usuario", "hash").Value!;
        cuenta.AddSucursal(_sucursal);
        return cuenta;
    }

    private Rol NewRol()
    {
        var rol = Rol.Create("Inventario", null).Value!;
        rol.AddSucursal(_sucursal);
        return rol;
    }

    private InventarioSucursal NewInventario() => InventarioSucursal.Create(_sucursal.Id, Guid.NewGuid(), 10m).Value!;

    private PosSesionCaja NewSesion() => PosSesionCaja.Create(_sucursal.Id, Guid.NewGuid(), DateTime.UtcNow, 100m).Value!;

    private SunatSerieFiscal NewSerie() => SunatSerieFiscal.Create(_sucursal.Id, TipoComprobante.Boleta, "B001", 0, SunatSerieFiscal.EstadoActiva).Value!;

    private Venta NewVenta() => Venta.Create(_sucursal.Id, Guid.NewGuid(), null, null, [DetalleVenta.Create(Guid.NewGuid(), 1m, 10m, 8m).Value!]).Value!;

    private Transferencia NewTransferencia(bool comoOrigen) =>
        Transferencia.Create(
            comoOrigen ? _sucursal.Id : Guid.NewGuid(),
            comoOrigen ? Guid.NewGuid() : _sucursal.Id,
            Guid.NewGuid(),
            [DetalleTransferencia.Create(Guid.NewGuid(), 5m).Value!]).Value!;

    private DigemidInventarioCuarentena NewCuarentena() =>
        DigemidInventarioCuarentena.Create(_sucursal.Id, Guid.NewGuid(), null, 1m, "Vencido", EstadoResolucionCuarentena.Retenido, Guid.NewGuid()).Value!;

    private MagistralOrdenProduccion NewOrden() =>
        MagistralOrdenProduccion.Create(_sucursal.Id, null, Guid.NewGuid(), null, null, Guid.NewGuid(), MagistralOrdenProduccion.EstadoIniciada).Value!;

    private async Task AssertConflictAsync(Dependents dependents)
    {
        SetupDependents(dependents);

        var result = await _handler.Handle(new DeleteSucursalCommand(_sucursal.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.InUse");
        _ = result.StatusCode.Should().Be(409);
        _sucursalesMock.Verify(m => m.Remove(It.IsAny<Sucursal>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenSucursalDoesNotExist()
    {
        _ = _sucursalesMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Sucursal?>(null));

        var result = await _handler.Handle(new DeleteSucursalCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByEmpleado() =>
        await AssertConflictAsync(new Dependents(Empleado: NewEmpleado()));

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenAssignedToCuenta() =>
        await AssertConflictAsync(new Dependents(Cuenta: NewCuenta()));

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenAssignedToRol() =>
        await AssertConflictAsync(new Dependents(Rol: NewRol()));

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByInventario() =>
        await AssertConflictAsync(new Dependents(Inventario: NewInventario()));

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedBySesionCaja() =>
        await AssertConflictAsync(new Dependents(Sesion: NewSesion()));

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedBySerieFiscal() =>
        await AssertConflictAsync(new Dependents(Serie: NewSerie()));

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByVenta() =>
        await AssertConflictAsync(new Dependents(Venta: NewVenta()));

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByTransferenciaAsOriginOrDestination(bool comoOrigen) =>
        await AssertConflictAsync(new Dependents(Transferencia: NewTransferencia(comoOrigen)));

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByCuarentena() =>
        await AssertConflictAsync(new Dependents(Cuarentena: NewCuarentena()));

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByOrdenMagistral() =>
        await AssertConflictAsync(new Dependents(Orden: NewOrden()));

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenAllDependentsAreSoftDeleted()
    {
        var empleado = NewEmpleado();
        var cuenta = NewCuenta();
        var rol = NewRol();
        var inventario = NewInventario();
        var sesion = NewSesion();
        var serie = NewSerie();
        var venta = NewVenta();
        var transferencia = NewTransferencia(true);
        var cuarentena = NewCuarentena();
        var orden = NewOrden();
        empleado.IsDeleted = cuenta.IsDeleted = rol.IsDeleted = inventario.IsDeleted = sesion.IsDeleted = true;
        serie.IsDeleted = venta.IsDeleted = transferencia.IsDeleted = cuarentena.IsDeleted = orden.IsDeleted = true;
        SetupDependents(new Dependents(empleado, cuenta, rol, inventario, sesion, serie, venta, transferencia, cuarentena, orden));

        var result = await _handler.Handle(new DeleteSucursalCommand(_sucursal.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _sucursalesMock.Verify(m => m.Remove(_sucursal), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenSucursalHasNoDependents()
    {
        var result = await _handler.Handle(new DeleteSucursalCommand(_sucursal.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _sucursalesMock.Verify(m => m.Remove(_sucursal), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
