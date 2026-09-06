using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Medicamentos.Commands.UpdateMedicamento;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Medicamentos.Commands.UpdateMedicamento;

public class UpdateMedicamentoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Medicamento>> _medicamentosMock;
    private readonly UpdateMedicamentoCommandHandler _handler;

    public UpdateMedicamentoCommandHandlerTests()
    {
        _medicamentosMock = new List<Medicamento>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(_medicamentosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateMedicamentoCommandHandler(_dbContextMock.Object);
    }

    private void SetupFind(Medicamento? entity) =>
        _medicamentosMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(entity));

    private static UpdateMedicamentoCommand Command(Guid id, string codigo = "COD-002") => new(
        id, codigo, "Nombre Nuevo", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.RecetaRetenida);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenMedicamentoDoesNotExist()
    {
        SetupFind(null);

        var result = await _handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndSave_WhenCommandValid()
    {
        var medicamento = Medicamento.Create("COD-001", "Viejo", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
        SetupFind(medicamento);

        var result = await _handler.Handle(Command(medicamento.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = medicamento.CodigoNacional.Should().Be("COD-002");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndNotSave_WhenDomainValidationFails()
    {
        var medicamento = Medicamento.Create("COD-001", "Viejo", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
        SetupFind(medicamento);

        var result = await _handler.Handle(Command(medicamento.Id, codigo: ""), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = medicamento.CodigoNacional.Should().Be("COD-001");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
