using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Recetas.Commands.DeleteReceta;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Recetas.Commands.DeleteReceta;

public class DeleteRecetaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<DbSet<RecetaMedica>> _recetasMock;
    private readonly RecetaMedica _receta;
    private readonly DeleteRecetaCommandHandler _handler;

    public DeleteRecetaCommandHandlerTests()
    {
        _receta = RecetaMedica.Create(Guid.NewGuid(), Guid.NewGuid(), DateOnly.FromDateTime(DateTime.UtcNow), 1).Value!;
        _recetasMock = new List<RecetaMedica> { _receta }.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Recetas).Returns(_recetasMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupDetallesVenta();
        _handler = new DeleteRecetaCommandHandler(_dbContextMock.Object);
    }

    private void SetupDetallesVenta(params DetalleVenta[] detalles) =>
        _dbContextMock.Setup(c => c.DetallesVenta).Returns(detalles.ToList().BuildMockDbSet().Object);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenRecetaDoesNotExist()
    {
        var result = await _handler.Handle(new DeleteRecetaCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenRecetaIsUsedInVentas()
    {
        SetupDetallesVenta(DetalleVenta.Create(Guid.NewGuid(), 1m, 10m, 5m, _receta.Id).Value!);

        var result = await _handler.Handle(new DeleteRecetaCommand(_receta.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("RecetaMedica.InUse");
        _ = result.StatusCode.Should().Be(409);
        _recetasMock.Verify(m => m.Remove(It.IsAny<RecetaMedica>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenRecetaIsNotUsed()
    {
        SetupDetallesVenta(DetalleVenta.Create(Guid.NewGuid(), 1m, 10m, 5m, Guid.NewGuid()).Value!);

        var result = await _handler.Handle(new DeleteRecetaCommand(_receta.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(_receta.Id);
        _recetasMock.Verify(m => m.Remove(_receta), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
