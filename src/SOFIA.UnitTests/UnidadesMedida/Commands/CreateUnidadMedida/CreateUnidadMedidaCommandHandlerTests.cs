using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.UnidadesMedida.Commands.CreateUnidadMedida;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.UnidadesMedida.Commands.CreateUnidadMedida;

public class CreateUnidadMedidaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<UnidadMedida> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<UnidadMedida>> _setMock;
    private readonly CreateUnidadMedidaCommandHandler _handler;

    public CreateUnidadMedidaCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateUnidadMedidaCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenCodigoIsTaken()
    {
        _existentes.Add(UnidadMedida.Create("K-001", "Tableta").Value!);

        var result = await _handler.Handle(new CreateUnidadMedidaCommand("K-001", "Tableta"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("UnidadMedida.Codigo.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _setMock.Verify(m => m.Add(It.IsAny<UnidadMedida>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenCodigoOnlyBelongsToADeletedRow()
    {
        var eliminado = UnidadMedida.Create("K-001", "Tableta").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CreateUnidadMedidaCommand("K-001", "Tableta"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _setMock.Verify(m => m.Add(It.IsAny<UnidadMedida>()), Times.Once);
    }
}
