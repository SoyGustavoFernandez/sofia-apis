using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.POS.Commands.CerrarCaja;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.POS.Commands.CerrarCaja;

public class CerrarCajaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly List<Venta> _ventas = [];
    private readonly List<VentaPago> _pagos = [];
    private readonly List<VentaReclamoSeguro> _reclamos = [];
    private readonly List<DevolucionCabecera> _devoluciones = [];

    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _empleadoId = Guid.NewGuid();
    private readonly PosSesionCaja _sesion;

    public CerrarCajaCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns(_sucursalId.ToString());
        _ = _currentUserMock.Setup(u => u.Id).Returns(_empleadoId.ToString());

        _sesion = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow.AddHours(-8), 100m).Value!;
    }

    private CerrarCajaCommandHandler CreateHandler()
    {
        _ = _dbContextMock.Setup(c => c.POSSesionesCaja).Returns(new List<PosSesionCaja> { _sesion }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(_ventas.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.VentasPagos).Returns(_pagos.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.VentasReclamosSeguro).Returns(_reclamos.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.DetallesVenta).Returns(_ventas.SelectMany(v => v.Detalles).ToList().BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Devoluciones).Returns(_devoluciones.BuildMockDbSet().Object);

        return new CerrarCajaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    private Venta AgregarVenta(decimal total, Guid? sesionId = null, decimal montoCubierto = 0, params (MetodoPago Metodo, decimal Monto)[] pagos)
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 1, total, 1).Value!;
        var venta = Venta.Create(_sucursalId, _empleadoId, null, sesionId ?? _sesion.Id, [detalle]).Value!;
        var ventaPagos = pagos.Select(p => VentaPago.Create(p.Metodo, p.Monto, null, DateTime.UtcNow).Value!).ToList();
        _ = venta.RegistrarPagos(ventaPagos, montoCubierto);

        if (montoCubierto > 0)
        {
            _reclamos.Add(VentaReclamoSeguro.Create(detalle.Id, Guid.NewGuid(), montoCubierto, total - montoCubierto, "Aprobado", "AUTH").Value!);
        }

        _ventas.Add(venta);
        _pagos.AddRange(ventaPagos);
        return venta;
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenSessionDoesNotExist()
    {
        var result = await CreateHandler().Handle(new CerrarCajaCommand(Guid.NewGuid(), 100m), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Caja");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenCallerDidNotOpenTheSessionAndIsNotAdmin()
    {
        _ = _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());

        var result = await CreateHandler().Handle(new CerrarCajaCommand(_sesion.Id, 100m), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.NoPropia");
        _ = result.StatusCode.Should().Be(403);
        _ = _sesion.EstadoSesion.Should().Be(EstadoSesion.Abierta);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAllowAdmin_ToCloseAnotherCashiersSession()
    {
        _ = _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());
        _ = _currentUserMock.Setup(u => u.IsInRole(Rol.AdminRoleName)).Returns(true);

        var result = await CreateHandler().Handle(new CerrarCajaCommand(_sesion.Id, 100m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesion.EstadoSesion.Should().Be(EstadoSesion.Cuadrada);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldComputeExpectedCashServerSide_AndRecordTheDifference()
    {
        _ = AgregarVenta(20m, pagos: (MetodoPago.Efectivo, 50m)); // 50 in, 30 change out
        _ = AgregarVenta(30m, pagos: (MetodoPago.Tarjeta, 30m)); // no cash
        var anulada = AgregarVenta(10m, pagos: (MetodoPago.Efectivo, 10m));
        _ = anulada.Anular("Error de digitación");
        _ = AgregarVenta(40m, sesionId: Guid.NewGuid(), pagos: (MetodoPago.Efectivo, 40m)); // another drawer

        var result = await CreateHandler().Handle(new CerrarCajaCommand(_sesion.Id, 115m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesion.MontoCierreCalculado.Should().Be(120m);
        _ = _sesion.MontoCierreDeclarado.Should().Be(115m);
        _ = _sesion.DiferenciaArqueo.Should().Be(-5m);
        _ = _sesion.EstadoSesion.Should().Be(EstadoSesion.Cerrada);
    }

    [Fact]
    public async Task Handle_ShouldDeductInsuranceCoverage_WhenComputingTheChangeGiven()
    {
        _ = AgregarVenta(20m, montoCubierto: 15m, pagos: (MetodoPago.Efectivo, 10m)); // copay 5, change 5

        var result = await CreateHandler().Handle(new CerrarCajaCommand(_sesion.Id, 105m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesion.MontoCierreCalculado.Should().Be(105m);
        _ = _sesion.DiferenciaArqueo.Should().Be(0m);
        _ = _sesion.EstadoSesion.Should().Be(EstadoSesion.Cuadrada);
    }

    [Fact]
    public async Task Handle_ShouldTakeTheChangeFromTheCash_WhenPaymentMixesCardAndCash()
    {
        _ = AgregarVenta(20m, pagos: [(MetodoPago.Tarjeta, 15m), (MetodoPago.Efectivo, 10m)]); // 10 in, 5 change out

        var result = await CreateHandler().Handle(new CerrarCajaCommand(_sesion.Id, 105m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesion.MontoCierreCalculado.Should().Be(105m);
        _ = _sesion.EstadoSesion.Should().Be(EstadoSesion.Cuadrada);
    }

    [Fact]
    public async Task Handle_ShouldExpectOnlyTheOpeningFloat_WhenSessionHasNoSales()
    {
        var result = await CreateHandler().Handle(new CerrarCajaCommand(_sesion.Id, 90m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesion.MontoCierreCalculado.Should().Be(100m);
        _ = _sesion.DiferenciaArqueo.Should().Be(-10m);
        _ = _sesion.EstadoSesion.Should().Be(EstadoSesion.Cerrada);
    }

    private void AgregarDevolucion(decimal monto, MetodoPago metodo, Guid? sesionId = null)
    {
        var detalle = DevolucionDetalle.Create(Guid.NewGuid(), 1m, DestinoDevolucion.Reingreso_Venta).Value!;
        var devolucion = DevolucionCabecera.Create(Guid.NewGuid(), null, _empleadoId, "07", "Motivo", DateTime.UtcNow, [detalle]).Value!;
        _ = devolucion.RegistrarReembolso(sesionId ?? _sesion.Id, monto, metodo);
        _devoluciones.Add(devolucion);
    }

    [Fact]
    public async Task Handle_ShouldSubtractOnlyThisSessionsCashRefunds()
    {
        _ = AgregarVenta(50m, pagos: (MetodoPago.Efectivo, 50m));
        AgregarDevolucion(20m, MetodoPago.Efectivo);
        AgregarDevolucion(15m, MetodoPago.Tarjeta); // not paid out of the drawer
        AgregarDevolucion(30m, MetodoPago.Efectivo, Guid.NewGuid()); // another drawer

        var result = await CreateHandler().Handle(new CerrarCajaCommand(_sesion.Id, 130m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesion.MontoCierreCalculado.Should().Be(130m); // 100 + 50 - 20
        _ = _sesion.EstadoSesion.Should().Be(EstadoSesion.Cuadrada);
    }

    [Fact]
    public async Task Handle_ShouldSubtractCashRefunds_WhenSessionHasNoSales()
    {
        AgregarDevolucion(25m, MetodoPago.Efectivo);

        var result = await CreateHandler().Handle(new CerrarCajaCommand(_sesion.Id, 75m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesion.MontoCierreCalculado.Should().Be(75m);
        _ = _sesion.EstadoSesion.Should().Be(EstadoSesion.Cuadrada);
    }
}
