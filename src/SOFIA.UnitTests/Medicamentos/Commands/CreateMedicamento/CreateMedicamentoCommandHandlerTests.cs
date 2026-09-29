using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Medicamentos.Commands.CreateMedicamento;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Medicamentos.Commands.CreateMedicamento;

public class CreateMedicamentoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Medicamento>> _medicamentosMock;
    private readonly CreateMedicamentoCommandHandler _handler;
    private static readonly Laboratorio Lab = Laboratorio.Create("Lab Uno", null).Value!;
    private static readonly UnidadMedida Und = UnidadMedida.Create("UND", "Unidad").Value!;

    public CreateMedicamentoCommandHandlerTests()
    {
        _medicamentosMock = new List<Medicamento>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(_medicamentosMock.Object);
        _ = _dbContextMock.Setup(c => c.Laboratorios).Returns(new List<Laboratorio> { Lab }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(new List<UnidadMedida> { Und }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateMedicamentoCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenLaboratorioIsUnknownOrForeign()
    {
        var command = new CreateMedicamentoCommand("COD-001", "Paracetamol", Guid.NewGuid(), Und.Id, CondicionVenta.RecetaSimple, 15m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Laboratorio.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _medicamentosMock.Verify(m => m.Add(It.IsAny<Medicamento>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenUnidadBaseIsUnknownOrForeign()
    {
        var command = new CreateMedicamentoCommand("COD-001", "Paracetamol", Lab.Id, Guid.NewGuid(), CondicionVenta.RecetaSimple, 15m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Error.Code.Should().Be("UnidadMedida.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _medicamentosMock.Verify(m => m.Add(It.IsAny<Medicamento>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAddAndSave_WhenCommandValid()
    {
        var command = new CreateMedicamentoCommand("COD-001", "Paracetamol", Lab.Id, Und.Id, CondicionVenta.RecetaSimple, 15m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(201);
        _ = result.Value.Should().NotBe(Guid.Empty);
        _medicamentosMock.Verify(m => m.Add(It.IsAny<Medicamento>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndNotSave_WhenDomainValidationFails()
    {
        var command = new CreateMedicamentoCommand("", "Paracetamol", Lab.Id, Und.Id, CondicionVenta.RecetaSimple, 15m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.CodigoNacional");
        _medicamentosMock.Verify(m => m.Add(It.IsAny<Medicamento>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenCodigoNacionalIsTaken()
    {
        var existente = Medicamento.Create("COD-001", "Otro", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { existente }.BuildMockDbSet().Object);

        var result = await _handler.Handle(new CreateMedicamentoCommand("COD-001", "Paracetamol", Lab.Id, Und.Id, CondicionVenta.RecetaSimple, 15m), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Medicamento.CodigoNacional.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenCodigoNacionalOnlyBelongsToADeletedMedicamento()
    {
        var eliminado = Medicamento.Create("COD-001", "Otro", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
        eliminado.IsDeleted = true;
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { eliminado }.BuildMockDbSet().Object);

        var result = await _handler.Handle(new CreateMedicamentoCommand("COD-001", "Paracetamol", Lab.Id, Und.Id, CondicionVenta.RecetaSimple, 15m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
