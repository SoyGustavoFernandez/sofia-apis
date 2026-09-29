using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Delivery.Commands.ProgramarDelivery;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Delivery.Commands.ProgramarDelivery;

public class ProgramarDeliveryCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ISucursalAccess> _sucursalAccessMock = new();
    private readonly List<DespachoDelivery> _despachos = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<DespachoDelivery>> _despachosMock;
    private readonly Venta _venta = NuevaVenta();
    private readonly ProgramarDeliveryCommandHandler _handler;

    public ProgramarDeliveryCommandHandlerTests()
    {
        _despachosMock = _despachos.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.DespachosDelivery).Returns(_despachosMock.Object);
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(new List<Venta> { _venta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _sucursalAccessMock.Setup(s => s.CanAccessAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>())).ReturnsAsync(true);
        _handler = new ProgramarDeliveryCommandHandler(_dbContextMock.Object, _sucursalAccessMock.Object);
    }

    private static Venta NuevaVenta() =>
        Venta.Create(Guid.NewGuid(), Guid.NewGuid(), null, null, [DetalleVenta.Create(Guid.NewGuid(), 1m, 10m, 8m).Value!]).Value!;

    private static ProgramarDeliveryCommand Command(Guid ventaId) =>
        new(ventaId, "Rappi", null, EstadoDespacho.Preparando, "Av. Uno 123", null, null);

    private void VerifyNothingSaved()
    {
        _despachosMock.Verify(m => m.Add(It.IsAny<DespachoDelivery>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAddAndSave_WhenSaleIsValidAndHasNoDispatch()
    {
        var result = await _handler.Handle(Command(_venta.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _despachosMock.Verify(m => m.Add(It.IsAny<DespachoDelivery>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenSaleIsUnknownOrForeign()
    {
        var result = await _handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Venta.NotFound");
        _ = result.StatusCode.Should().Be(404);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ShouldReturnForbiddenAndNotSave_WhenSaleBranchIsNotAllowed()
    {
        _ = _sucursalAccessMock.Setup(s => s.CanAccessAsync(_venta.SucursalId, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await _handler.Handle(Command(_venta.Id), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Delivery.Venta.SucursalNoPermitida");
        _ = result.StatusCode.Should().Be(403);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenSaleIsCancelled()
    {
        _ = _venta.Anular("Error de digitacion");

        var result = await _handler.Handle(Command(_venta.Id), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Delivery.Venta.Anulada");
        _ = result.StatusCode.Should().Be(409);
        VerifyNothingSaved();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenSaleAlreadyHasADispatch()
    {
        _despachos.Add(DespachoDelivery.Create(_venta.Id, "Glovo", null, EstadoDespacho.Preparando, "Av. Uno 123", null, null).Value!);

        var result = await _handler.Handle(Command(_venta.Id), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Delivery.Venta.YaProgramado");
        _ = result.StatusCode.Should().Be(409);
        VerifyNothingSaved();
    }
}
