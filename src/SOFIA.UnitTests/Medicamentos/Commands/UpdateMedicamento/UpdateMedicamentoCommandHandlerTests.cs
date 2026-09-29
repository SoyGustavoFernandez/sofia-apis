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
    private static readonly Laboratorio Lab = Laboratorio.Create("Lab Uno", null).Value!;
    private static readonly UnidadMedida Und = UnidadMedida.Create("UND", "Unidad").Value!;

    public UpdateMedicamentoCommandHandlerTests()
    {
        _medicamentosMock = new List<Medicamento>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(_medicamentosMock.Object);
        _ = _dbContextMock.Setup(c => c.Laboratorios).Returns(new List<Laboratorio> { Lab }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(new List<UnidadMedida> { Und }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateMedicamentoCommandHandler(_dbContextMock.Object);
    }

    private void SetupFind(Medicamento? entity) =>
        _medicamentosMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(entity));

    private static UpdateMedicamentoCommand Command(Guid id, string codigo = "COD-002", Guid? laboratorioId = null, Guid? unidadId = null) => new(
        id, codigo, "Nombre Nuevo", laboratorioId ?? Lab.Id, unidadId ?? Und.Id, CondicionVenta.RecetaRetenida, 25m);

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenLaboratorioIsUnknownOrForeign()
    {
        var medicamento = Medicamento.Create("COD-001", "Viejo", Lab.Id, Und.Id, CondicionVenta.VentaLibreOTC).Value!;
        SetupFind(medicamento);

        var result = await _handler.Handle(Command(medicamento.Id, laboratorioId: Guid.NewGuid()), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Laboratorio.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = medicamento.LaboratorioId.Should().Be(Lab.Id);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenUnidadBaseIsUnknownOrForeign()
    {
        var medicamento = Medicamento.Create("COD-001", "Viejo", Lab.Id, Und.Id, CondicionVenta.VentaLibreOTC).Value!;
        SetupFind(medicamento);

        var result = await _handler.Handle(Command(medicamento.Id, unidadId: Guid.NewGuid()), CancellationToken.None);

        _ = result.Error.Code.Should().Be("UnidadMedida.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSkipLookups_WhenReferencesAreUnchanged()
    {
        var laboratorioId = Guid.NewGuid();
        var unidadId = Guid.NewGuid();
        var medicamento = Medicamento.Create("COD-001", "Viejo", laboratorioId, unidadId, CondicionVenta.VentaLibreOTC).Value!;
        SetupFind(medicamento);

        var result = await _handler.Handle(Command(medicamento.Id, laboratorioId: laboratorioId, unidadId: unidadId), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.Laboratorios, Times.Never);
        _dbContextMock.Verify(c => c.UnidadesMedida, Times.Never);
    }

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

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotChange_WhenCodigoNacionalBelongsToAnotherMedicamento()
    {
        var medicamento = Medicamento.Create("COD-001", "Viejo", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
        var otro = Medicamento.Create("COD-002", "Otro", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
        var setMock = new List<Medicamento> { medicamento, otro }.BuildMockDbSet();
        _ = setMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<Medicamento?>(medicamento));
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(setMock.Object);

        var result = await _handler.Handle(Command(medicamento.Id, codigo: "COD-002"), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Medicamento.CodigoNacional.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = medicamento.CodigoNacional.Should().Be("COD-001");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenKeepingItsOwnCodigoNacional()
    {
        var medicamento = Medicamento.Create("COD-001", "Viejo", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
        var setMock = new List<Medicamento> { medicamento }.BuildMockDbSet();
        _ = setMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<Medicamento?>(medicamento));
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(setMock.Object);

        var result = await _handler.Handle(Command(medicamento.Id, codigo: "COD-001"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
