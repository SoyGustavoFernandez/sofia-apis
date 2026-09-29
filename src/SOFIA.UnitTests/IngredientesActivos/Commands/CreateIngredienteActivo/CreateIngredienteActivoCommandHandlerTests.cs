using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.IngredientesActivos.Commands.CreateIngredienteActivo;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.IngredientesActivos.Commands.CreateIngredienteActivo;

public class CreateIngredienteActivoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<IngredienteActivo> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<IngredienteActivo>> _setMock;
    private readonly CreateIngredienteActivoCommandHandler _handler;

    public CreateIngredienteActivoCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.IngredientesActivos).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateIngredienteActivoCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenDciIsTaken()
    {
        _existentes.Add(IngredienteActivo.Create("K-001", "N02BE01").Value!);

        var result = await _handler.Handle(new CreateIngredienteActivoCommand("K-001", "N02BE01"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("IngredienteActivo.DenominacionDci.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _setMock.Verify(m => m.Add(It.IsAny<IngredienteActivo>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenDciOnlyBelongsToADeletedRow()
    {
        var eliminado = IngredienteActivo.Create("K-001", "N02BE01").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CreateIngredienteActivoCommand("K-001", "N02BE01"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _setMock.Verify(m => m.Add(It.IsAny<IngredienteActivo>()), Times.Once);
    }
}
