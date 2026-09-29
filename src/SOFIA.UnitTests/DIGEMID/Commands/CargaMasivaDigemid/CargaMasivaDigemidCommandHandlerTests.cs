using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.DIGEMID.Commands.CargaMasivaDigemid;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.DIGEMID.Commands.CargaMasivaDigemid;

public class CargaMasivaDigemidCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<DigemidCatalogoProducto> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<DigemidCatalogoProducto>> _setMock;
    private readonly CargaMasivaDigemidCommandHandler _handler;

    public CargaMasivaDigemidCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.DigemidCatalogoProductos).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CargaMasivaDigemidCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseKeyExistsInTheDatabaseOrEarlierInTheFile()
    {
        _existentes.Add(DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo").Value!);
        var command = new CargaMasivaDigemidCommand([new("k-001", "Paracetamol", null, null, null, null, null, "Activo"), new("K-002", "Paracetamol", null, null, null, null, null, "Activo"), new("k-002", "Paracetamol", null, null, null, null, null, "Activo"), new("K-003", "Paracetamol", null, null, null, null, null, "Activo")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(2);
        _setMock.Verify(m => m.Add(It.IsAny<DigemidCatalogoProducto>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSaveRow_WhenTheKeyOnlyBelongsToADeletedRow()
    {
        var eliminado = DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CargaMasivaDigemidCommand([new("K-001", "Paracetamol", null, null, null, null, null, "Activo")]), CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }
}
