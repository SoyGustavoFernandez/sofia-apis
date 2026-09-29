using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.IngredientesActivos.Commands.UpdateIngredienteActivo;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.IngredientesActivos.Commands.UpdateIngredienteActivo;

public class UpdateIngredienteActivoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly IngredienteActivo _entity = IngredienteActivo.Create("K-001", "N02BE01").Value!;
    private readonly List<IngredienteActivo> _existentes;
    private readonly UpdateIngredienteActivoCommandHandler _handler;

    public UpdateIngredienteActivoCommandHandlerTests()
    {
        _existentes = [_entity];
        var setMock = _existentes.BuildMockDbSet();
        _ = setMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<IngredienteActivo?>(_entity));
        _ = _dbContextMock.Setup(c => c.IngredientesActivos).Returns(setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateIngredienteActivoCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenKeepingItsOwnDci()
    {
        var result = await _handler.Handle(new UpdateIngredienteActivoCommand(_entity.Id, "K-001", "N02BE01"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotChange_WhenDciBelongsToAnotherRow()
    {
        _existentes.Add(IngredienteActivo.Create("K-002", "N02BE01").Value!);

        var result = await _handler.Handle(new UpdateIngredienteActivoCommand(_entity.Id, "K-002", "N02BE01"), CancellationToken.None);

        _ = result.Error.Code.Should().Be("IngredienteActivo.DenominacionDci.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = _entity.DenominacionDci.Should().Be("K-001");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenDciOnlyBelongsToADeletedRow()
    {
        var eliminado = IngredienteActivo.Create("K-002", "N02BE01").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new UpdateIngredienteActivoCommand(_entity.Id, "K-002", "N02BE01"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
