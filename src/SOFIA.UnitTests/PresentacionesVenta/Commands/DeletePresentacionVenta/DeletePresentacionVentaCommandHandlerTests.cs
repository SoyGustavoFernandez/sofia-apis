using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.PresentacionesVenta.Commands.DeletePresentacionVenta;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.PresentacionesVenta.Commands.DeletePresentacionVenta;

public class DeletePresentacionVentaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<PresentacionVenta>> _presentacionesMock;
    private readonly DeletePresentacionVentaCommandHandler _handler;

    public DeletePresentacionVentaCommandHandlerTests()
    {
        _presentacionesMock = new List<PresentacionVenta>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.PresentacionesVenta).Returns(_presentacionesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupDetallesVenta();

        _handler = new DeletePresentacionVentaCommandHandler(_dbContextMock.Object);
    }

    private void SetupFind(PresentacionVenta? presentacion) =>
        _presentacionesMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(presentacion));

    private void SetupDetallesVenta(params DetalleVenta[] detalles) =>
        _dbContextMock.Setup(c => c.DetallesVenta).Returns(detalles.ToList().BuildMockDbSet().Object);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPresentacionDoesNotExist()
    {
        SetupFind(null);

        var result = await _handler.Handle(new DeletePresentacionVentaCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenPresentacionHasVentas()
    {
        var presentacion = PresentacionVenta.Create(Guid.NewGuid(), Guid.NewGuid(), "Caja x10", 10m, 45m).Value!;
        SetupFind(presentacion);
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 20m, 45m, 30m, null, presentacion.Id, 2m).Value!;
        SetupDetallesVenta(detalle);

        var result = await _handler.Handle(new DeletePresentacionVentaCommand(presentacion.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.InUse");
        _ = result.StatusCode.Should().Be(409);
        _presentacionesMock.Verify(m => m.Remove(It.IsAny<PresentacionVenta>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenPresentacionHasNoVentas()
    {
        var presentacion = PresentacionVenta.Create(Guid.NewGuid(), Guid.NewGuid(), "Caja x10", 10m, 45m).Value!;
        SetupFind(presentacion);
        var detalle = DetalleVenta.Create(Guid.NewGuid(), 20m, 45m, 30m, null, Guid.NewGuid(), 2m).Value!;
        SetupDetallesVenta(detalle);

        var result = await _handler.Handle(new DeletePresentacionVentaCommand(presentacion.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _presentacionesMock.Verify(m => m.Remove(presentacion), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
