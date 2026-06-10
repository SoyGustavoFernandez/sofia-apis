using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Medicamentos.Commands.DeleteMedicamento;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Medicamentos.Commands.DeleteMedicamento;

public class DeleteMedicamentoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<DbSet<Medicamento>> _medicamentosDbSetMock;
    private readonly DeleteMedicamentoCommandHandler _handler;

    public DeleteMedicamentoCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _medicamentosDbSetMock = new Mock<DbSet<Medicamento>>();
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(_medicamentosDbSetMock.Object);
        _handler = new DeleteMedicamentoCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenMedicamentoDoesNotExist()
    {
        // Arrange
        _medicamentosDbSetMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Medicamento?>(null));

        var command = new DeleteMedicamentoCommand(Guid.NewGuid());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldCallRemoveAndSave_WhenMedicamentoExists()
    {
        // Arrange
        var medicamento = Medicamento.Create(
            "COD-001", "Paracetamol 500mg",
            Guid.NewGuid(), Guid.NewGuid(),
            CondicionVenta.VentaLibreOTC).Value!;

        _medicamentosDbSetMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Medicamento?>(medicamento));

        _ = _dbContextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new DeleteMedicamentoCommand(medicamento.Id);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _medicamentosDbSetMock.Verify(m => m.Remove(medicamento), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotCallSave_WhenMedicamentoNotFound()
    {
        // Arrange
        _medicamentosDbSetMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Medicamento?>(null));

        var command = new DeleteMedicamentoCommand(Guid.NewGuid());

        // Act
        _ = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
