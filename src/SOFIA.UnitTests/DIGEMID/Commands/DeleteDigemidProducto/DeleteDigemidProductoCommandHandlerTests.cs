using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.DIGEMID.Commands.DeleteDigemidProducto;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.DIGEMID.Commands.DeleteDigemidProducto;

public class DeleteDigemidProductoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<DigemidCatalogoProducto>> _setMock = new List<DigemidCatalogoProducto>().BuildMockDbSet();
    private readonly DeleteDigemidProductoCommandHandler _handler;

    public DeleteDigemidProductoCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.DigemidCatalogoProductos).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new DeleteDigemidProductoCommandHandler(_dbContextMock.Object);
    }

    private void ReturnOnFind(DigemidCatalogoProducto? entity) =>
        _ = _setMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult(entity));

    [Fact]
    public async Task Handle_ShouldRemove_WhenRowBelongsToTheCompany()
    {
        var propio = DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo", Guid.NewGuid()).Value!;
        ReturnOnFind(propio);

        var result = await _handler.Handle(new DeleteDigemidProductoCommand(propio.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _setMock.Verify(m => m.Remove(propio), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbiddenAndNotRemove_WhenRowBelongsToTheSharedCatalog()
    {
        var global = DigemidCatalogoProducto.Create("G-001", "Ibuprofeno", null, null, null, null, null, "Activo").Value!;
        ReturnOnFind(global);

        var result = await _handler.Handle(new DeleteDigemidProductoCommand(global.Id), CancellationToken.None);

        _ = result.Error.Code.Should().Be("DigemidCatalogo.Global.SoloLectura");
        _ = result.StatusCode.Should().Be(403);
        _setMock.Verify(m => m.Remove(It.IsAny<DigemidCatalogoProducto>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenRowDoesNotExist()
    {
        ReturnOnFind(null);

        var result = await _handler.Handle(new DeleteDigemidProductoCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.StatusCode.Should().Be(404);
    }
}
