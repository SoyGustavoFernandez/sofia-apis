using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.UnidadesMedida.Commands.CargaMasivaUnidadesMedida;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.UnidadesMedida.Commands.CargaMasivaUnidadesMedida;

public class CargaMasivaUnidadesMedidaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<UnidadMedida> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<UnidadMedida>> _setMock;
    private readonly CargaMasivaUnidadesMedidaCommandHandler _handler;

    public CargaMasivaUnidadesMedidaCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CargaMasivaUnidadesMedidaCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseKeyExistsInTheDatabaseOrEarlierInTheFile()
    {
        _existentes.Add(UnidadMedida.Create("K-001", "Tableta").Value!);
        var command = new CargaMasivaUnidadesMedidaCommand([new("k-001", "Tableta"), new("K-002", "Tableta"), new("k-002", "Tableta"), new("K-003", "Tableta")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(2);
        _setMock.Verify(m => m.Add(It.IsAny<UnidadMedida>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSaveRow_WhenTheKeyOnlyBelongsToADeletedRow()
    {
        var eliminado = UnidadMedida.Create("K-001", "Tableta").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CargaMasivaUnidadesMedidaCommand([new("K-001", "Tableta")]), CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }
}
