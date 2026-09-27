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
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _empleadoSesionId = Guid.NewGuid();
    private readonly Guid _sucursalId = Guid.NewGuid();

    private readonly List<Venta> _ventas = [];
    private readonly List<InventarioSucursal> _inventarios = [];
    private readonly List<SunatSerieFiscal> _series = [];
    private readonly List<SunatComprobanteEmitido> _comprobantes = [];
    private readonly List<PosSesionCaja> _sesiones = [];
    private readonly List<DevolucionCabecera> _devoluciones = [];
    private readonly List<VentaReclamoSeguro> _reclamos = [];
    private readonly List<DevolucionCabecera> _devolucionesAgregadas = [];
    private readonly List<SunatComprobanteEmitido> _comprobantesAgregados = [];

    private readonly SunatSerieFiscal _serieBoleta;
    private readonly SunatSerieFiscal _serieNc;
    private readonly PosSesionCaja _sesion;

    public ProcesarDevolucionCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.Id).Returns(_empleadoSesionId.ToString());
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns(_sucursalId.ToString());

        _serieBoleta = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.Boleta, "B001", 10, "Activa").Value!;
        _serieNc = SunatSerieFiscal.Create(_sucursalId, TipoComprobante.NotaCredito, "BC01", 100, "Activa").Value!;
        _series.AddRange([_serieBoleta, _serieNc]);

        _sesion = PosSesionCaja.Create(_sucursalId, _empleadoSesionId, DateTime.UtcNow.AddHours(-2), 100m).Value!;
        _sesiones.Add(_sesion);
    }

    private ProcesarDevolucionCommandHandler CreateHandler()
    {
        _ = _dbContextMock.Setup(x => x.Ventas).Returns(_ventas.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(x => x.LotesEnSucursal).Returns(_inventarios.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(x => x.SUNATSeriesFiscales).Returns(_series.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(x => x.POSSesionesCaja).Returns(_sesiones.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(x => x.VentasReclamosSeguro).Returns(_reclamos.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(x => x.DetallesDevolucion).Returns(_devoluciones.SelectMany(d => d.Detalles).ToList().BuildMockDbSet().Object);

        var devolucionesMock = _devoluciones.BuildMockDbSet();
        _ = devolucionesMock.Setup(d => d.Add(It.IsAny<DevolucionCabecera>())).Callback<DevolucionCabecera>(_devolucionesAgregadas.Add);
        _ = _dbContextMock.Setup(x => x.Devoluciones).Returns(devolucionesMock.Object);

        var comprobantesMock = _comprobantes.BuildMockDbSet();
        _ = comprobantesMock.Setup(c => c.Add(It.IsAny<SunatComprobanteEmitido>())).Callback<SunatComprobanteEmitido>(_comprobantesAgregados.Add);
        _ = _dbContextMock.Setup(x => x.SUNATComprobantesEmitidos).Returns(comprobantesMock.Object);

        _ = _dbContextMock.Setup(x => x.IncrementarCorrelativoSunatAsync(_serieNc.Id, It.IsAny<CancellationToken>())).ReturnsAsync(101);

        return new ProcesarDevolucionCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    private Venta AgregarVentaEmitida(Guid? sucursalId = null, params DetalleVenta[] detalles)
    {
        var venta = Venta.Create(sucursalId ?? _sucursalId, Guid.NewGuid(), null, _sesion.Id, [.. detalles]).Value!;
        _ventas.Add(venta);
        _comprobantes.Add(SunatComprobanteEmitido.Create(
            venta.Id, _serieBoleta.Id, 11, "1", "12345678", "JUAN PEREZ",
            venta.MontoTotalBruto * 0.82m, 0, venta.MontoTotalBruto * 0.18m, venta.MontoTotalBruto,
            "HASH", "Aceptado", null, null, null).Value!);
        return venta;
    }

    private void AgregarDevolucionPrevia(Guid comprobanteId, Guid detalleVentaId, decimal cantidad)
    {
        var detalle = DevolucionDetalle.Create(detalleVentaId, cantidad, DestinoDevolucion.Reingreso_Venta).Value!;
        _devoluciones.Add(DevolucionCabecera.Create(comprobanteId, null, Guid.NewGuid(), "07", "Previa", DateTime.UtcNow, [detalle]).Value!);
    }

    private static ProcesarDevolucionCommand Comando(Guid ventaId, params DevolucionDetalleDto[] detalles) =>
        new(ventaId, "07", "Devolución", [.. detalles]);

    [Fact]
    public async Task Handle_WhenSessionHasNoEmployee_ShouldReturnFailure()
    {
        _ = _currentUserMock.Setup(u => u.Id).Returns((string?)null);

        var result = await CreateHandler().Handle(Comando(Guid.NewGuid(), new DevolucionDetalleDto(Guid.NewGuid(), 1m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Auth.Empleado");
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVentaNotFound_ShouldReturnFailure()
    {
        var result = await CreateHandler().Handle(Comando(Guid.NewGuid(), new DevolucionDetalleDto(Guid.NewGuid(), 1m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.NotFound");
    }

    [Fact]
    public async Task Handle_WhenVentaBelongsToAnotherBranch_ShouldReturnForbidden()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = AgregarVentaEmitida(Guid.NewGuid(), detalle);

        var result = await CreateHandler().Handle(Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 1m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Venta.OtraSucursal");
        _ = result.StatusCode.Should().Be(403);
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenCallerHasNoOpenSession_ShouldReturnFailure()
    {
        _sesiones.Clear();
        _sesiones.Add(PosSesionCaja.Create(_sucursalId, Guid.NewGuid(), DateTime.UtcNow, 50m).Value!); // another cashier's drawer
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = AgregarVentaEmitida(null, detalle);

        var result = await CreateHandler().Handle(Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 1m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Caja.SinSesionAbierta");
    }

    [Fact]
    public async Task Handle_WhenVentaIsAnulada_ShouldReturnFailure()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = AgregarVentaEmitida(null, detalle);
        _ = venta.Anular("Error");

        var result = await CreateHandler().Handle(Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 1m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Venta.EstadoInvalido");
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenSaleHasNoIssuedComprobante_ShouldReturnFailure()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = AgregarVentaEmitida(null, detalle);
        _comprobantes.Clear();

        var result = await CreateHandler().Handle(Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 1m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.ComprobanteOrigen.NoEmitido");
    }

    [Fact]
    public async Task Handle_WhenNoActiveCreditNoteSerie_ShouldFailInsteadOfSkippingTheNC()
    {
        _ = _series.Remove(_serieNc);
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = AgregarVentaEmitida(null, detalle);

        var result = await CreateHandler().Handle(Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 1m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.SerieNotaCredito.NoConfigurada");
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDetalleNotFound_ShouldReturnFailure()
    {
        var venta = AgregarVentaEmitida(null, DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!);

        var result = await CreateHandler().Handle(Comando(venta.Id, new DevolucionDetalleDto(Guid.NewGuid(), 1m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("DetalleVenta.NotFound");
    }

    [Fact]
    public async Task Handle_WhenCantidadExceedsVendida_ShouldReturnFailure()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = AgregarVentaEmitida(null, detalle);

        var result = await CreateHandler().Handle(Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 6m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Cantidad.Excedida");
    }

    [Fact]
    public async Task Handle_WhenPreviousReturnsPlusRequestedExceedSold_ShouldReturnFailure()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = AgregarVentaEmitida(null, detalle);
        AgregarDevolucionPrevia(_comprobantes[0].Id, detalle.Id, 4m);

        var result = await CreateHandler().Handle(Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 2m, DestinoDevolucion.Reingreso_Venta)), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Cantidad.Excedida");
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTheSameLineIsRepeatedInTheRequest_ShouldCapTheirSum()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = AgregarVentaEmitida(null, detalle);

        var result = await CreateHandler().Handle(
            Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 3m, DestinoDevolucion.Reingreso_Venta), new DevolucionDetalleDto(detalle.Id, 3m, DestinoDevolucion.Cuarentena_DIGEMID)),
            CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Devolucion.Cantidad.Excedida");
    }

    [Fact]
    public async Task Handle_WhenValidPartialReturn_ShouldCreditOnlyTheReturnedLinesAndRefundThroughTheSession()
    {
        var loteId1 = Guid.NewGuid();
        var loteId2 = Guid.NewGuid();
        var detalle1 = DetalleVenta.Create(loteId1, 5m, 10m, 8m).Value!;
        var detalle2 = DetalleVenta.Create(loteId2, 2m, 20m, 15m).Value!;
        var venta = AgregarVentaEmitida(null, detalle1, detalle2); // total 90
        var inventario1 = InventarioSucursal.Create(_sucursalId, loteId1, 50m).Value!;
        var inventario2 = InventarioSucursal.Create(_sucursalId, loteId2, 20m).Value!;
        _inventarios.AddRange([inventario1, inventario2]);

        var result = await CreateHandler().Handle(
            Comando(venta.Id, new DevolucionDetalleDto(detalle1.Id, 2m, DestinoDevolucion.Reingreso_Venta), new DevolucionDetalleDto(detalle2.Id, 1m, DestinoDevolucion.Cuarentena_DIGEMID)),
            CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = inventario1.CantidadFisica.Should().Be(52m);
        _ = inventario2.CantidadFisica.Should().Be(20m);
        _ = venta.Estado.Should().Be(EstadoVenta.Completada);

        var devolucion = _devolucionesAgregadas.Should().ContainSingle().Subject;
        var nc = _comprobantesAgregados.Should().ContainSingle().Subject;
        _ = devolucion.ComprobanteOrigenId.Should().Be(_comprobantes[0].Id);
        _ = devolucion.ComprobanteNcId.Should().Be(nc.Id);
        _ = devolucion.EmpleadoAutorizaId.Should().Be(_empleadoSesionId);
        _ = devolucion.SesionId.Should().Be(_sesion.Id);
        _ = devolucion.MontoReembolsado.Should().Be(40m);
        _ = devolucion.MetodoReembolso.Should().Be(MetodoPago.Efectivo);

        _ = nc.NumeroCorrelativo.Should().Be(101);
        _ = nc.SerieId.Should().Be(_serieNc.Id);
        _ = nc.MontoTotalVenta.Should().Be(40m); // 2 x 10 + 1 x 20
        _ = nc.MontoGravadoIgv.Should().Be(40m * 0.82m);
        _ = nc.MontoTotalIgv.Should().Be(40m * 0.18m);
        _ = nc.RazonSocialCliente.Should().Be("JUAN PEREZ");
        _dbContextMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTheLastUnitsAreReturned_ShouldMarkTheSaleDevuelta()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 5m, 10m, 8m).Value!;
        var venta = AgregarVentaEmitida(null, detalle);
        AgregarDevolucionPrevia(_comprobantes[0].Id, detalle.Id, 3m);

        var result = await CreateHandler().Handle(Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 2m, DestinoDevolucion.Cuarentena_DIGEMID)), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = venta.Estado.Should().Be(EstadoVenta.Devuelta);
    }

    [Fact]
    public async Task Handle_WhenInsuranceCoveredPartOfTheSale_ShouldRefundOnlyTheCustomerShare()
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 4m, 25m, 8m).Value!;
        var venta = AgregarVentaEmitida(null, detalle); // total 100, insurer paid 75
        _reclamos.Add(VentaReclamoSeguro.Create(detalle.Id, Guid.NewGuid(), 75m, 25m, "Aprobado", "AUTH").Value!);

        var result = await CreateHandler().Handle(
            Comando(venta.Id, new DevolucionDetalleDto(detalle.Id, 2m, DestinoDevolucion.Cuarentena_DIGEMID)) with { MetodoReembolso = MetodoPago.Tarjeta },
            CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _comprobantesAgregados.Single().MontoTotalVenta.Should().Be(50m);
        var devolucion = _devolucionesAgregadas.Single();
        _ = devolucion.MontoReembolsado.Should().Be(12.5m);
        _ = devolucion.MetodoReembolso.Should().Be(MetodoPago.Tarjeta);
    }
}
