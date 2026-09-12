using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.FormulacionesClinicas.Commands.Delete;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.FormulacionesClinicas.Commands.DeleteFormulacionClinica;

public class DeleteFormulacionClinicaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly DeleteFormulacionClinicaCommandHandler _handler;

    public DeleteFormulacionClinicaCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new DeleteFormulacionClinicaCommandHandler(_dbContextMock.Object);
    }

    private void SetupFormulaciones(params FormulacionClinica[] items) =>
        _dbContextMock.Setup(c => c.FormulacionesClinicas).Returns(items.ToList().BuildMockDbSet().Object);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenFormulacionDoesNotExist()
    {
        SetupFormulaciones();

        var result = await _handler.Handle(new DeleteFormulacionClinicaCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Formulacion.NotFound");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenFormulacionExists()
    {
        var formulacion = FormulacionClinica.Create(Guid.NewGuid(), Guid.NewGuid(), 100m, Guid.NewGuid(), null).Value!;
        var mockSet = new List<FormulacionClinica> { formulacion }.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.FormulacionesClinicas).Returns(mockSet.Object);

        var result = await _handler.Handle(new DeleteFormulacionClinicaCommand(formulacion.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        mockSet.Verify(m => m.Remove(formulacion), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
