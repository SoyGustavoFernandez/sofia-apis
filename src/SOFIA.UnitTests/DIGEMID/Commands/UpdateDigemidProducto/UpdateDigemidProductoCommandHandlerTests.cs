using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.DIGEMID.Commands.UpdateDigemidProducto;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.DIGEMID.Commands.UpdateDigemidProducto;

public class UpdateDigemidProductoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly DigemidCatalogoProducto _entity = DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo").Value!;
    private readonly List<DigemidCatalogoProducto> _existentes;
    private readonly UpdateDigemidProductoCommandHandler _handler;

    public UpdateDigemidProductoCommandHandlerTests()
    {
        _existentes = [_entity];
        var setMock = _existentes.BuildMockDbSet();
        _ = setMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<DigemidCatalogoProducto?>(_entity));
        _ = _dbContextMock.Setup(c => c.DigemidCatalogoProductos).Returns(setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateDigemidProductoCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenKeepingItsOwnCodProd()
    {
        var result = await _handler.Handle(new UpdateDigemidProductoCommand(_entity.Id, "K-001", "Paracetamol", null, null, null, null, null, "Activo"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotChange_WhenCodProdBelongsToAnotherRow()
    {
        _existentes.Add(DigemidCatalogoProducto.Create("K-002", "Paracetamol", null, null, null, null, null, "Activo").Value!);

        var result = await _handler.Handle(new UpdateDigemidProductoCommand(_entity.Id, "K-002", "Paracetamol", null, null, null, null, null, "Activo"), CancellationToken.None);

        _ = result.Error.Code.Should().Be("DigemidCatalogo.CodProd.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = _entity.CodProd.Should().Be("K-001");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenCodProdOnlyBelongsToADeletedRow()
    {
        var eliminado = DigemidCatalogoProducto.Create("K-002", "Paracetamol", null, null, null, null, null, "Activo").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new UpdateDigemidProductoCommand(_entity.Id, "K-002", "Paracetamol", null, null, null, null, null, "Activo"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
