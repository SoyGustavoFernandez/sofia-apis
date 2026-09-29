using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.UnidadesMedida.Commands.UpdateUnidadMedida;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.UnidadesMedida.Commands.UpdateUnidadMedida;

public class UpdateUnidadMedidaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly UnidadMedida _entity = UnidadMedida.Create("K-001", "Tableta").Value!;
    private readonly List<UnidadMedida> _existentes;
    private readonly UpdateUnidadMedidaCommandHandler _handler;

    public UpdateUnidadMedidaCommandHandlerTests()
    {
        _existentes = [_entity];
        var setMock = _existentes.BuildMockDbSet();
        _ = setMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<UnidadMedida?>(_entity));
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateUnidadMedidaCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenKeepingItsOwnCodigo()
    {
        var result = await _handler.Handle(new UpdateUnidadMedidaCommand(_entity.Id, "K-001", "Tableta"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotChange_WhenCodigoBelongsToAnotherRow()
    {
        _existentes.Add(UnidadMedida.Create("K-002", "Tableta").Value!);

        var result = await _handler.Handle(new UpdateUnidadMedidaCommand(_entity.Id, "K-002", "Tableta"), CancellationToken.None);

        _ = result.Error.Code.Should().Be("UnidadMedida.Codigo.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = _entity.Codigo.Should().Be("K-001");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenCodigoOnlyBelongsToADeletedRow()
    {
        var eliminado = UnidadMedida.Create("K-002", "Tableta").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new UpdateUnidadMedidaCommand(_entity.Id, "K-002", "Tableta"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
