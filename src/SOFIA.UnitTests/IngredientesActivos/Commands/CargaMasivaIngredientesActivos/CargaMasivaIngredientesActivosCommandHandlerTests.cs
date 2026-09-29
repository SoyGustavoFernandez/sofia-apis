using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.IngredientesActivos.Commands.CargaMasivaIngredientesActivos;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.IngredientesActivos.Commands.CargaMasivaIngredientesActivos;

public class CargaMasivaIngredientesActivosCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<IngredienteActivo> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<IngredienteActivo>> _setMock;
    private readonly CargaMasivaIngredientesActivosCommandHandler _handler;

    public CargaMasivaIngredientesActivosCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.IngredientesActivos).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CargaMasivaIngredientesActivosCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseKeyExistsInTheDatabaseOrEarlierInTheFile()
    {
        _existentes.Add(IngredienteActivo.Create("K-001", "N02BE01").Value!);
        var command = new CargaMasivaIngredientesActivosCommand([new("k-001", "N02BE01"), new("K-002", "N02BE01"), new("k-002", "N02BE01"), new("K-003", "N02BE01")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(2);
        _setMock.Verify(m => m.Add(It.IsAny<IngredienteActivo>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSaveRow_WhenTheKeyOnlyBelongsToADeletedRow()
    {
        var eliminado = IngredienteActivo.Create("K-001", "N02BE01").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CargaMasivaIngredientesActivosCommand([new("K-001", "N02BE01")]), CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }
}
