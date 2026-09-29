using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.JerarquiasUoM.Commands.CargaMasivaJerarquiasUoM;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.JerarquiasUoM.Commands.CargaMasivaJerarquiasUoM;

public class CargaMasivaJerarquiasUoMCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<JerarquiaUoM> _jerarquias = [];
    private readonly List<Medicamento> _medicamentos = [];
    private readonly UnidadMedida _caja = UnidadMedida.Create("CJ", "Caja").Value!;
    private readonly UnidadMedida _blister = UnidadMedida.Create("BL", "Blister").Value!;
    private readonly UnidadMedida _tableta = UnidadMedida.Create("TAB", "Tableta").Value!;
    private readonly List<UnidadMedida> _unidades;
    private readonly Medicamento _paracetamol;
    private readonly CargaMasivaJerarquiasUoMCommandHandler _handler;

    public CargaMasivaJerarquiasUoMCommandHandlerTests()
    {
        _unidades = [_caja, _blister, _tableta];
        _paracetamol = NewMedicamento("COD-1", "Paracetamol");
        _medicamentos.Add(_paracetamol);

        var jerarquiasMock = _jerarquias.BuildMockDbSet();
        _ = jerarquiasMock.Setup(d => d.Add(It.IsAny<JerarquiaUoM>())).Callback<JerarquiaUoM>(_jerarquias.Add);
        _ = _dbContextMock.Setup(c => c.JerarquiasUoM).Returns(jerarquiasMock.Object);
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(() => _medicamentos.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(() => _unidades.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CargaMasivaJerarquiasUoMCommandHandler(_dbContextMock.Object);
    }

    private static Medicamento NewMedicamento(string codigo, string nombre) =>
        Medicamento.Create(codigo, nombre, Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;

    private static JerarquiaUoMImportRow Row(string mayor, string menor, string multiplicador, string producto = "Paracetamol") =>
        new(producto, mayor, menor, multiplicador);

    [Fact]
    public async Task Handle_ShouldSaveConsistentChain()
    {
        var command = new CargaMasivaJerarquiasUoMCommand([Row("Caja", "Blister", "4"), Row("Blister", "Tableta", "10"), Row("Caja", "Tableta", "40")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(3);
        _ = _jerarquias.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldSkipRow_WhenItContradictsAnEarlierRowInTheFile()
    {
        var command = new CargaMasivaJerarquiasUoMCommand([Row("Caja", "Blister", "4"), Row("Blister", "Tableta", "10"), Row("Caja", "Tableta", "50")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(2);
        _ = _jerarquias.Should().NotContain(j => j.UnidadMayorId == _caja.Id && j.UnidadMenorId == _tableta.Id);
    }

    [Fact]
    public async Task Handle_ShouldSkipRow_WhenItContradictsAnExistingConversion()
    {
        _jerarquias.Add(JerarquiaUoM.Create(_paracetamol.Id, _caja.Id, _blister.Id, 4m).Value!);
        var command = new CargaMasivaJerarquiasUoMCommand([Row("Blister", "Caja", "2")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(0);
        _ = _jerarquias.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ShouldSkipRow_WhenTheSameEdgeIsRepeated()
    {
        _jerarquias.Add(JerarquiaUoM.Create(_paracetamol.Id, _caja.Id, _blister.Id, 4m).Value!);
        var command = new CargaMasivaJerarquiasUoMCommand([Row("Caja", "Blister", "4"), Row("Blister", "Tableta", "10"), Row("Blister", "Tableta", "10")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(1);
        _ = _jerarquias.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldSkipAmbiguousNamesInsteadOfThrowing_WhenCatalogNamesRepeat()
    {
        _medicamentos.Add(NewMedicamento("COD-2", "PARACETAMOL"));
        _medicamentos.Add(NewMedicamento("COD-3", "Ibuprofeno"));
        _unidades.Add(UnidadMedida.Create("CJ2", "caja").Value!);
        var command = new CargaMasivaJerarquiasUoMCommand(
        [
            Row("Blister", "Tableta", "10"),
            Row("Blister", "Tableta", "10", producto: "Ibuprofeno"),
            Row("Caja", "Tableta", "10", producto: "Ibuprofeno"),
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(1);
        _ = _jerarquias.Should().ContainSingle().Which.ProductoId.Should().NotBe(_paracetamol.Id);
    }
}
