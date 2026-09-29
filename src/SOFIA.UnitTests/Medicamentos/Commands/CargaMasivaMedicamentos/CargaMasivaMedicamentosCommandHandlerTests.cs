using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Medicamentos.Commands.CargaMasivaMedicamentos;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Medicamentos.Commands.CargaMasivaMedicamentos;

public class CargaMasivaMedicamentosCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Medicamento>> _medicamentosMock;
    private readonly Laboratorio _lab = Laboratorio.Create("Bayer S.A.", null).Value!;
    private readonly UnidadMedida _unidad = UnidadMedida.Create("TAB", "Tableta").Value!;
    private readonly CargaMasivaMedicamentosCommandHandler _handler;

    public CargaMasivaMedicamentosCommandHandlerTests()
    {
        _medicamentosMock = new List<Medicamento>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(_medicamentosMock.Object);
        _ = _dbContextMock.Setup(c => c.Laboratorios).Returns(new List<Laboratorio> { _lab }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(new List<UnidadMedida> { _unidad }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CargaMasivaMedicamentosCommandHandler(_dbContextMock.Object);
    }

    private MedicamentoImportRow Row(
        string codigo = "COD-001",
        string nombre = "Paracetamol",
        string? lab = "Bayer S.A.",
        string? unidad = "Tableta",
        string condicion = "Venta Libre (OTC)") => new(codigo, nombre, lab!, unidad!, condicion);

    [Fact]
    public async Task Handle_ShouldSaveAllValidRows()
    {
        var command = new CargaMasivaMedicamentosCommand([Row("COD-001"), Row("COD-002")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(2);
        _medicamentosMock.Verify(m => m.Add(It.IsAny<Medicamento>()), Times.Exactly(2));
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldMatchCatalogsCaseInsensitively()
    {
        var command = new CargaMasivaMedicamentosCommand([Row(lab: "BAYER S.A.", unidad: "tableta", condicion: "venta libre (otc)")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }

    [Fact]
    public async Task Handle_ShouldSkipRow_WhenLaboratorioUnknown()
    {
        var command = new CargaMasivaMedicamentosCommand([Row(lab: "Inexistente")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(0);
        _medicamentosMock.Verify(m => m.Add(It.IsAny<Medicamento>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSkipRow_WhenUnidadUnknown()
    {
        var command = new CargaMasivaMedicamentosCommand([Row(unidad: "Inexistente")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldSkipRow_WhenCondicionVentaInvalid()
    {
        var command = new CargaMasivaMedicamentosCommand([Row(condicion: "No existe")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ShouldSkipOnlyAmbiguousRows_WhenUnidadDescripcionIsRepeated()
    {
        var capsula = UnidadMedida.Create("CAP", "Capsula").Value!;
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(new List<UnidadMedida>
        {
            _unidad,
            UnidadMedida.Create("TAB2", "TABLETA").Value!,
            capsula,
        }.BuildMockDbSet().Object);
        var command = new CargaMasivaMedicamentosCommand([Row("COD-001", unidad: "Tableta"), Row("COD-002", unidad: "Capsula")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(1);
        _medicamentosMock.Verify(m => m.Add(It.Is<Medicamento>(x => x.CodigoNacional == "COD-002" && x.UnidadBaseId == capsula.Id)), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldSaveOnlyValidRows_WhenMixed()
    {
        var command = new CargaMasivaMedicamentosCommand(
        [
            Row("COD-001"),
            Row("COD-002", lab: "Inexistente"),
            Row("COD-003"),
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(2);
    }

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseCodigoExistsInTheDatabaseOrEarlierInTheFile()
    {
        var existente = Medicamento.Create("COD-001", "Existente", _lab.Id, _unidad.Id, SOFIA.Domain.Enums.CondicionVenta.VentaLibreOTC).Value!;
        var eliminado = Medicamento.Create("COD-009", "Eliminado", _lab.Id, _unidad.Id, SOFIA.Domain.Enums.CondicionVenta.VentaLibreOTC).Value!;
        eliminado.IsDeleted = true;
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { existente, eliminado }.BuildMockDbSet().Object);
        var command = new CargaMasivaMedicamentosCommand([Row("cod-001"), Row("COD-002"), Row("cod-002"), Row("COD-009")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(2, because: "COD-001 exists and the second COD-002 repeats the file, while COD-009 was deleted");
    }
}
