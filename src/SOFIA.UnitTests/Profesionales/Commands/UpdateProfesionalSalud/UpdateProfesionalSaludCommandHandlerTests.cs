using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Profesionales.Commands.UpdateProfesionalSalud;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Profesionales.Commands.UpdateProfesionalSalud;

public class UpdateProfesionalSaludCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly ProfesionalSalud _entity = ProfesionalSalud.Create("K-001", "Dra. Rios", null).Value!;
    private readonly List<ProfesionalSalud> _existentes;
    private readonly UpdateProfesionalSaludCommandHandler _handler;

    public UpdateProfesionalSaludCommandHandlerTests()
    {
        _existentes = [_entity];
        var setMock = _existentes.BuildMockDbSet();
        _ = setMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<ProfesionalSalud?>(_entity));
        _ = _dbContextMock.Setup(c => c.ProfesionalesSalud).Returns(setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateProfesionalSaludCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenKeepingItsOwnRegistro()
    {
        var result = await _handler.Handle(new UpdateProfesionalSaludCommand(_entity.Id, "K-001", "Dra. Rios", null), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotChange_WhenRegistroBelongsToAnotherRow()
    {
        _existentes.Add(ProfesionalSalud.Create("K-002", "Dra. Rios", null).Value!);

        var result = await _handler.Handle(new UpdateProfesionalSaludCommand(_entity.Id, "K-002", "Dra. Rios", null), CancellationToken.None);

        _ = result.Error.Code.Should().Be("ProfesionalSalud.NumeroRegistro.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = _entity.NumeroRegistro.Should().Be("K-001");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenRegistroOnlyBelongsToADeletedRow()
    {
        var eliminado = ProfesionalSalud.Create("K-002", "Dra. Rios", null).Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new UpdateProfesionalSaludCommand(_entity.Id, "K-002", "Dra. Rios", null), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
