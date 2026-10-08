using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.AnularVenta;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Commands.AnularVenta;

public class AnularVentaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _empleadoId = Guid.NewGuid();
    private readonly Guid _loteId = Guid.NewGuid();
    private readonly List<DevolucionCabecera> _devoluciones = [];
    private readonly List<SunatComprobanteEmitido> _comprobantes = [];
    private readonly List<SunatSerieFiscal> _series = [];
    private readonly List<PosSesionCaja> _sesiones = [];
    private readonly InventarioSucursal _inventario;
    private readonly DetalleVenta _detalle;
    private readonly PosSesionCaja _sesionPropia;
    private Venta _venta;

    public AnularVentaCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns(_sucursalId.ToString());
        _ = _currentUserMock.Setup(u => u.Id).Returns(_empleadoId.ToString());

        _detalle = DetalleVenta.Create(_loteId, 5m, 10m, 8m).Value!;
        _venta = Venta.Create(_sucursalId, Guid.NewGuid(), null, Guid.NewGuid(), [_detalle]).Value!;
        _inventario = InventarioSucursal.Create(_sucursalId, _loteId, 20m).Value!;
        _sesionPropia = PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow.AddHours(-1), 100m).Value!;
        _sesiones.Add(_sesionPropia);
    }

    private AnularVentaCommandHandler CreateHandler()
    {
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(new List<Venta> { _venta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.POSSesionesCaja).Returns(_sesiones.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.LotesEnSucursal).Returns(new List<InventarioSucursal> { _inventario }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Devoluciones).Returns(_devoluciones.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SUNATComprobantesEmitidos).Returns(_comprobantes.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SUNATSeriesFiscales).Returns(_series.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.DetallesDevolucion).Returns(_devoluciones.SelectMany(d => d.Detalles).ToList().BuildMockDbSet().Object);

        return new AnularVentaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldVoidTheSaleAndRestoreStock_WhenItHasNoReturns()
    {
        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Error de digitación"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _venta.Estado.Should().Be(EstadoVenta.Anulada);
        _ = _inventario.CantidadFisica.Should().Be(25m);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldStoreTheCallersOwnOpenSession_WhenTheSaleWasCharged()
    {
        var sesionCobro = _venta.SesionId;

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Error de digitación"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _venta.Estado.Should().Be(EstadoVenta.Anulada);
        _ = _venta.SesionAnulacionId.Should().Be(_sesionPropia.Id);
        _ = _venta.SesionId.Should().Be(sesionCobro);
    }

    [Fact]
    public async Task Handle_ShouldRejectAndChangeNothing_WhenTheSaleWasChargedAndTheCallerHasNoOpenSession()
    {
        _sesiones.Clear();
        var boleta = Emitir(TipoComprobante.Boleta, "B001");

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Error de digitación"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Anular.SinSesionAbierta");
        _ = result.Error.Type.Should().Be(ErrorType.Validation);
        _ = _venta.Estado.Should().Be(EstadoVenta.Completada);
        _ = _venta.SesionAnulacionId.Should().BeNull();
        _ = _venta.MotivoAnulacion.Should().BeNull();
        _ = boleta.EstadoAceptacion.Should().Be(EstadoComprobanteSunat.NoEnviado);
        _ = _inventario.CantidadFisica.Should().Be(20m);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenTheOnlyOpenSessionBelongsToAnotherEmployee()
    {
        _sesiones.Clear();
        _sesiones.Add(PosSesionCaja.Create(_sucursalId, Guid.NewGuid(), DateTime.UtcNow.AddHours(-1), 100m).Value!);

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Error de digitación"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Anular.SinSesionAbierta");
        _ = _venta.Estado.Should().Be(EstadoVenta.Completada);
        _ = _inventario.CantidadFisica.Should().Be(20m);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenTheCallersOwnSessionIsClosedOrInAnotherBranch()
    {
        _ = _sesionPropia.Cerrar(DateTime.UtcNow, 100m, 100m);
        _sesiones.Add(PosSesionCaja.Create(Guid.NewGuid(), _empleadoId, DateTime.UtcNow.AddHours(-1), 100m).Value!);

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Error de digitación"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Anular.SinSesionAbierta");
        _ = _venta.Estado.Should().Be(EstadoVenta.Completada);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldVoidAParkedSaleWithoutAnySession_AsBefore()
    {
        _sesiones.Clear();
        _ = _currentUserMock.Setup(u => u.Id).Returns((string?)null);
        _venta = Venta.Create(_sucursalId, Guid.NewGuid(), null, Guid.NewGuid(), [_detalle], EstadoVenta.Pendiente).Value!;

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Cliente nunca volvió"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _venta.Estado.Should().Be(EstadoVenta.Anulada);
        _ = _venta.SesionAnulacionId.Should().BeNull();
        _ = _inventario.CantidadFisica.Should().Be(25m);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReportTheSaleAsAlreadyVoided_WhenTheCallerHasNoOpenSession()
    {
        _ = _venta.Anular("Error", Guid.NewGuid());
        _sesiones.Clear();

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Otra vez"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Anular");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReject_WhenTheSaleHasReturns()
    {
        var detalleDev = DevolucionDetalle.Create(_detalle.Id, 2m, DestinoDevolucion.Reingreso_Venta).Value!;
        _devoluciones.Add(DevolucionCabecera.Create(Guid.NewGuid(), null, Guid.NewGuid(), "07", "Motivo", DateTime.UtcNow, [detalleDev]).Value!);

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Cliente"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Anular.ConDevoluciones");
        _ = _venta.Estado.Should().Be(EstadoVenta.Completada);
        _ = _inventario.CantidadFisica.Should().Be(20m);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenTheSaleBelongsToAnotherBranch()
    {
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns(Guid.NewGuid().ToString());

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Cliente"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Anular");
        _ = _venta.Estado.Should().Be(EstadoVenta.Completada);
    }

    private SunatComprobanteEmitido Emitir(TipoComprobante tipo, string prefijo)
    {
        var serie = SunatSerieFiscal.Create(_sucursalId, tipo, prefijo, 0, SunatSerieFiscal.EstadoActiva).Value!;
        _series.Add(serie);
        var comprobante = SunatComprobanteEmitido.Create(
            _venta.Id, serie.Id, 1, TipoDocumentoIdentidad.Varios, "00000000", "CLIENTE EVENTUAL",
            42.37m, 0, 7.63m, 50m, null, EstadoComprobanteSunat.NoEnviado, null, null, null).Value!;
        _comprobantes.Add(comprobante);
        return comprobante;
    }

    [Fact]
    public async Task Handle_ShouldVoidTheSalesBoleta_ButNotOtherDocuments()
    {
        var boleta = Emitir(TipoComprobante.Boleta, "B001");
        var notaCredito = Emitir(TipoComprobante.NotaCredito, "BC01");

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Error de digitación"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = boleta.EstadoAceptacion.Should().Be(EstadoComprobanteSunat.Anulado);
        _ = notaCredito.EstadoAceptacion.Should().Be(EstadoComprobanteSunat.NoEnviado);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldKeepTheBoletaUntouched_WhenTheVoidIsRejected()
    {
        var boleta = Emitir(TipoComprobante.Boleta, "B001");
        var detalleDev = DevolucionDetalle.Create(_detalle.Id, 1m, DestinoDevolucion.Reingreso_Venta).Value!;
        _devoluciones.Add(DevolucionCabecera.Create(boleta.Id, null, Guid.NewGuid(), "07", "Motivo", DateTime.UtcNow, [detalleDev]).Value!);

        var result = await CreateHandler().Handle(new AnularVentaCommand(_venta.Id, "Cliente"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = boleta.EstadoAceptacion.Should().Be(EstadoComprobanteSunat.NoEnviado);
    }
}
