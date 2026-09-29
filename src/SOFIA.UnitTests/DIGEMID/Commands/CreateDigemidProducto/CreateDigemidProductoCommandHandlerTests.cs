using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.DIGEMID.Commands.CreateDigemidProducto;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.DIGEMID.Commands.CreateDigemidProducto;

public class CreateDigemidProductoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<DigemidCatalogoProducto> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<DigemidCatalogoProducto>> _setMock;
    private readonly CreateDigemidProductoCommandHandler _handler;

    public CreateDigemidProductoCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.DigemidCatalogoProductos).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateDigemidProductoCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenCodProdIsTaken()
    {
        _existentes.Add(DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo").Value!);

        var result = await _handler.Handle(new CreateDigemidProductoCommand("K-001", "Paracetamol", null, null, null, null, null, "Activo"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("DigemidCatalogo.CodProd.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _setMock.Verify(m => m.Add(It.IsAny<DigemidCatalogoProducto>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenCodProdOnlyBelongsToADeletedRow()
    {
        var eliminado = DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CreateDigemidProductoCommand("K-001", "Paracetamol", null, null, null, null, null, "Activo"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _setMock.Verify(m => m.Add(It.IsAny<DigemidCatalogoProducto>()), Times.Once);
    }
}
