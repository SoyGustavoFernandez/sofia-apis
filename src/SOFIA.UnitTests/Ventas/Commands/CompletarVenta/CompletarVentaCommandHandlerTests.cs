using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Ventas.Commands.CompletarVenta;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Ventas.Commands.CompletarVenta;

public class CompletarVentaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CompletarVentaCommandHandler _handler;
    private readonly List<VentaReclamoSeguro> _reclamosList = [];

    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _empleadoId = Guid.NewGuid();

    public CompletarVentaCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();

        _ = _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(c => c.SucursalId).Returns(_sucursalId.ToString());
        _ = _currentUserMock.Setup(c => c.Id).Returns(_empleadoId.ToString());

        _handler = new CompletarVentaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    private Venta CrearVentaPendiente(Guid? sucursalId = null)
    {
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 2, 10, 5).Value!;
        return Venta.Create(sucursalId ?? _sucursalId, _empleadoId, null, Guid.NewGuid(), [detalle], EstadoVenta.Pendiente).Value!;
    }

    private void SetupMocks(List<Venta> ventas, List<SunatSerieFiscal>? series = null, List<AseguradoraMedica>? aseguradoras = null)
    {
        var ventasDbSetMock = ventas.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(ventasDbSetMock.Object);

        _ = _dbContextMock.Setup(c => c.Aseguradoras).Returns((aseguradoras ?? []).BuildMockDbSet().Object);

        _ = _dbContextMock.Setup(c => c.SUNATSeriesFiscales).Returns((series ?? []).BuildMockDbSet().Object);

        var pagosList = new List<VentaPago>();
        var pagosDbSetMock = pagosList.BuildMockDbSet();
        _ = pagosDbSetMock.Setup(d => d.Add(It.IsAny<VentaPago>())).Callback<VentaPago>(pagosList.Add);
        _ = _dbContextMock.Setup(c => c.VentasPagos).Returns(pagosDbSetMock.Object);

        var comprobantesList = new List<SunatComprobanteEmitido>();
        var comprobantesDbSetMock = comprobantesList.BuildMockDbSet();
        _ = comprobantesDbSetMock.Setup(d => d.Add(It.IsAny<SunatComprobanteEmitido>())).Callback<SunatComprobanteEmitido>(comprobantesList.Add);
        _ = _dbContextMock.Setup(c => c.SUNATComprobantesEmitidos).Returns(comprobantesDbSetMock.Object);

        var reclamosDbSetMock = _reclamosList.BuildMockDbSet();
        _ = reclamosDbSetMock.Setup(d => d.Add(It.IsAny<VentaReclamoSeguro>())).Callback<VentaReclamoSeguro>(_reclamosList.Add);
        _ = _dbContextMock.Setup(c => c.VentasReclamosSeguro).Returns(reclamosDbSetMock.Object);

        var outboxList = new List<SistemaOutboxEvento>();
        var outboxDbSetMock = outboxList.BuildMockDbSet();
        _ = outboxDbSetMock.Setup(d => d.Add(It.IsAny<SistemaOutboxEvento>())).Callback<SistemaOutboxEvento>(outboxList.Add);
        _ = _dbContextMock.Setup(c => c.SistemaOutboxEventos).Returns(outboxDbSetMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenVentaDoesNotExist()
    {
        // Arrange
        SetupMocks([]);
        var command = new CompletarVentaCommand(Guid.NewGuid(), [new CreateVentaPagoDto(MetodoPago.Efectivo, 20, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Completar");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenVentaBelongsToAnotherSucursal()
    {
        // Arrange
        var venta = CrearVentaPendiente(sucursalId: Guid.NewGuid());
        SetupMocks([venta]);
        var command = new CompletarVentaCommand(venta.Id, [new CreateVentaPagoDto(MetodoPago.Efectivo, 20, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Completar");
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenVentaIsNotPendiente()
    {
        // Arrange
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 2, 10, 5).Value!;
        var venta = Venta.Create(_sucursalId, _empleadoId, null, Guid.NewGuid(), [detalle]).Value!; // Completada by default
        SetupMocks([venta]);
        var command = new CompletarVentaCommand(venta.Id, [new CreateVentaPagoDto(MetodoPago.Efectivo, 20, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Completar");
    }

    [Fact]
    public async Task Handle_ShouldCompletePendingSale_AndIssueComprobante_WhenPaymentCoversTheTotal()
    {
        // Arrange
        var venta = CrearVentaPendiente();
        SetupMocks([venta]);
        var command = new CompletarVentaCommand(venta.Id, [new CreateVentaPagoDto(MetodoPago.Efectivo, 20, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Comprobante.Should().NotBeNull();
        _ = venta.Estado.Should().Be(EstadoVenta.Completada);
        _ = venta.Pagos.Should().ContainSingle();

        // The new VentaPago must be explicitly Add()-ed: Venta was fetched (tracked, not Added),
        // so relationship fixup alone would mark it Unchanged and EF would try to UPDATE a non-existent row.
        _dbContextMock.Verify(c => c.VentasPagos.Add(It.IsAny<VentaPago>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenPaymentIsInsufficient()
    {
        // Arrange
        var venta = CrearVentaPendiente();
        SetupMocks([venta]);
        var command = new CompletarVentaCommand(venta.Id, [new CreateVentaPagoDto(MetodoPago.Efectivo, 5, null)]);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Pagos");
        _ = venta.Estado.Should().Be(EstadoVenta.Pendiente);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenAseguradoraDoesNotExist()
    {
        // Arrange
        var venta = CrearVentaPendiente();
        SetupMocks([venta]);
        var command = new CompletarVentaCommand(venta.Id, [new CreateVentaPagoDto(MetodoPago.Efectivo, 5, null)], AseguradoraId: Guid.NewGuid(), MontoCubiertoSeguro: 15);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Aseguradora.NotFound");
        _ = venta.Estado.Should().Be(EstadoVenta.Pendiente);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreCoverage_WhenNoAseguradoraIsProvided()
    {
        // Arrange
        var venta = CrearVentaPendiente();
        SetupMocks([venta]);
        var command = new CompletarVentaCommand(venta.Id, [new CreateVentaPagoDto(MetodoPago.Efectivo, 0.01m, null)], MontoCubiertoSeguro: 19.99m);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeFalse();
        _ = result.Error.Code.Should().Be("Venta.Pagos");
        _ = _reclamosList.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenCoverageExceedsTheSaleTotal()
    {
        // Arrange
        var venta = CrearVentaPendiente();
        var aseguradora = AseguradoraMedica.Create("Rimac", "RIMAC-01").Value!;
        SetupMocks([venta], aseguradoras: [aseguradora]);
        var command = new CompletarVentaCommand(venta.Id, [new CreateVentaPagoDto(MetodoPago.Efectivo, 0.01m, null)], AseguradoraId: aseguradora.Id, MontoCubiertoSeguro: 25);

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
        var venta = CrearVentaPendiente();
        var aseguradora = AseguradoraMedica.Create("Rimac", "RIMAC-01").Value!;
        SetupMocks([venta], aseguradoras: [aseguradora]);
        var command = new CompletarVentaCommand(venta.Id, [new CreateVentaPagoDto(MetodoPago.Efectivo, 5, null)], AseguradoraId: aseguradora.Id, MontoCubiertoSeguro: 15);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = _reclamosList.Should().ContainSingle().Which.MontoCopagoPaciente.Should().Be(5);
    }
}
