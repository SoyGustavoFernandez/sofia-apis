using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Profesionales.Commands.CreateProfesionalSalud;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Profesionales.Commands.CreateProfesionalSalud;

public class CreateProfesionalSaludCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<ProfesionalSalud> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<ProfesionalSalud>> _setMock;
    private readonly CreateProfesionalSaludCommandHandler _handler;

    public CreateProfesionalSaludCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.ProfesionalesSalud).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateProfesionalSaludCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenRegistroIsTaken()
    {
        _existentes.Add(ProfesionalSalud.Create("K-001", "Dra. Rios", null).Value!);

        var result = await _handler.Handle(new CreateProfesionalSaludCommand("K-001", "Dra. Rios", null), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ProfesionalSalud.NumeroRegistro.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _setMock.Verify(m => m.Add(It.IsAny<ProfesionalSalud>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenRegistroOnlyBelongsToADeletedRow()
    {
        var eliminado = ProfesionalSalud.Create("K-001", "Dra. Rios", null).Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CreateProfesionalSaludCommand("K-001", "Dra. Rios", null), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _setMock.Verify(m => m.Add(It.IsAny<ProfesionalSalud>()), Times.Once);
    }
}
