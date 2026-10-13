using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Profesionales.Commands.DeleteProfesionalSalud;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Profesionales.Commands.DeleteProfesionalSalud;

public class DeleteProfesionalSaludCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<DbSet<ProfesionalSalud>> _profesionalesMock = new();
    private readonly ProfesionalSalud _profesional = ProfesionalSalud.Create("K-001", "Dra. Rios", null).Value!;
    private readonly DeleteProfesionalSaludCommandHandler _handler;

    public DeleteProfesionalSaludCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.ProfesionalesSalud).Returns(_profesionalesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _profesionalesMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<ProfesionalSalud?>(_profesional));
        SetupRecetas();
        _handler = new DeleteProfesionalSaludCommandHandler(_dbContextMock.Object);
    }

    private void SetupRecetas(params RecetaMedica[] recetas) =>
        _dbContextMock.Setup(c => c.Recetas).Returns(recetas.ToList().BuildMockDbSet().Object);

    private RecetaMedica NewReceta() => RecetaMedica.Create(Guid.NewGuid(), _profesional.Id, new DateOnly(2026, 1, 1)).Value!;

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProfesionalDoesNotExist()
    {
        _ = _profesionalesMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<ProfesionalSalud?>(null));

        var result = await _handler.Handle(new DeleteProfesionalSaludCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ProfesionalSalud.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByReceta()
    {
        SetupRecetas(NewReceta());

        var result = await _handler.Handle(new DeleteProfesionalSaludCommand(_profesional.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("ProfesionalSalud.InUse");
        _ = result.StatusCode.Should().Be(409);
        _profesionalesMock.Verify(m => m.Remove(It.IsAny<ProfesionalSalud>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenOnlySoftDeletedRecetasReferenceIt()
    {
        var receta = NewReceta();
        receta.IsDeleted = true;
        SetupRecetas(receta);

        var result = await _handler.Handle(new DeleteProfesionalSaludCommand(_profesional.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _profesionalesMock.Verify(m => m.Remove(_profesional), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenProfesionalHasNoDependents()
    {
        var result = await _handler.Handle(new DeleteProfesionalSaludCommand(_profesional.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _profesionalesMock.Verify(m => m.Remove(_profesional), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
