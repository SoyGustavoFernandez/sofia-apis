using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Laboratorios.Commands.DeleteLaboratorio;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Laboratorios.Commands.DeleteLaboratorio;

public class DeleteLaboratorioCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Laboratorio>> _laboratoriosMock;
    private readonly DeleteLaboratorioCommandHandler _handler;

    public DeleteLaboratorioCommandHandlerTests()
    {
        _laboratoriosMock = new List<Laboratorio>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Laboratorios).Returns(_laboratoriosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupMedicamentos();
        _handler = new DeleteLaboratorioCommandHandler(_dbContextMock.Object);
    }

    private void SetupFind(Laboratorio? lab) =>
        _laboratoriosMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(lab));

    private void SetupMedicamentos(params Medicamento[] medicamentos) =>
        _dbContextMock.Setup(c => c.Medicamentos).Returns(medicamentos.ToList().BuildMockDbSet().Object);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLaboratorioDoesNotExist()
    {
        SetupFind(null);

        var result = await _handler.Handle(new DeleteLaboratorioCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Laboratorio.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenLaboratorioHasMedicamentos()
    {
        var lab = Laboratorio.Create("Bayer S.A.", null).Value!;
        SetupFind(lab);
        SetupMedicamentos(Medicamento.Create("COD-001", "Aspirina", lab.Id, Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!);

        var result = await _handler.Handle(new DeleteLaboratorioCommand(lab.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Laboratorio.InUse");
        _ = result.StatusCode.Should().Be(409);
        _laboratoriosMock.Verify(m => m.Remove(It.IsAny<Laboratorio>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenLaboratorioHasNoMedicamentos()
    {
        var lab = Laboratorio.Create("Bayer S.A.", null).Value!;
        SetupFind(lab);
        SetupMedicamentos(Medicamento.Create("COD-001", "Otro", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!);

        var result = await _handler.Handle(new DeleteLaboratorioCommand(lab.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _laboratoriosMock.Verify(m => m.Remove(lab), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
