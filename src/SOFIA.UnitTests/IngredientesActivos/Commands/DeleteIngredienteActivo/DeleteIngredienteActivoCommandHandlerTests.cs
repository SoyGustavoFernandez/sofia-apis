using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.IngredientesActivos.Commands.DeleteIngredienteActivo;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.IngredientesActivos.Commands.DeleteIngredienteActivo;

public class DeleteIngredienteActivoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<DbSet<IngredienteActivo>> _ingredientesMock = new();
    private readonly IngredienteActivo _ingrediente = IngredienteActivo.Create("Paracetamol", "N02BE01").Value!;
    private readonly DeleteIngredienteActivoCommandHandler _handler;

    public DeleteIngredienteActivoCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.IngredientesActivos).Returns(_ingredientesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _ingredientesMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<IngredienteActivo?>(_ingrediente));
        SetupFormulaciones();
        _handler = new DeleteIngredienteActivoCommandHandler(_dbContextMock.Object);
    }

    private void SetupFormulaciones(params FormulacionClinica[] formulaciones) =>
        _dbContextMock.Setup(c => c.FormulacionesClinicas).Returns(formulaciones.ToList().BuildMockDbSet().Object);

    private FormulacionClinica NewFormulacion() => FormulacionClinica.Create(Guid.NewGuid(), _ingrediente.Id, 500m, Guid.NewGuid(), null).Value!;

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenIngredienteDoesNotExist()
    {
        _ = _ingredientesMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<IngredienteActivo?>(null));

        var result = await _handler.Handle(new DeleteIngredienteActivoCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("IngredienteActivo.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByFormulacion()
    {
        SetupFormulaciones(NewFormulacion());

        var result = await _handler.Handle(new DeleteIngredienteActivoCommand(_ingrediente.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("IngredienteActivo.InUse");
        _ = result.StatusCode.Should().Be(409);
        _ingredientesMock.Verify(m => m.Remove(It.IsAny<IngredienteActivo>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenOnlySoftDeletedFormulacionesReferenceIt()
    {
        var formulacion = NewFormulacion();
        formulacion.IsDeleted = true;
        SetupFormulaciones(formulacion);

        var result = await _handler.Handle(new DeleteIngredienteActivoCommand(_ingrediente.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _ingredientesMock.Verify(m => m.Remove(_ingrediente), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenIngredienteHasNoDependents()
    {
        var result = await _handler.Handle(new DeleteIngredienteActivoCommand(_ingrediente.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _ingredientesMock.Verify(m => m.Remove(_ingrediente), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
