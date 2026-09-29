using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.JerarquiasUoM.Commands.CreateJerarquiaUoM;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.JerarquiasUoM.Commands.CreateJerarquiaUoM;

public class CreateJerarquiaUoMCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<JerarquiaUoM> _jerarquiasList = [];
    private readonly CreateJerarquiaUoMCommandHandler _handler;

    private readonly Guid _productoId;
    private readonly Guid _caja;
    private readonly Guid _blister;
    private readonly Guid _unidad;

    public CreateJerarquiaUoMCommandHandlerTests()
    {
        var producto = Medicamento.Create("COD-001", "Paracetamol", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
        var unidades = new List<UnidadMedida> { UnidadMedida.Create("CJA", "Caja").Value!, UnidadMedida.Create("BLI", "Blister").Value!, UnidadMedida.Create("UND", "Unidad").Value! };
        (_productoId, _caja, _blister, _unidad) = (producto.Id, unidades[0].Id, unidades[1].Id, unidades[2].Id);
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { producto }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(unidades.BuildMockDbSet().Object);

        var jerarquiasMock = _jerarquiasList.BuildMockDbSet();
        _ = jerarquiasMock.Setup(d => d.Add(It.IsAny<JerarquiaUoM>())).Callback<JerarquiaUoM>(_jerarquiasList.Add);
        _ = _dbContextMock.Setup(c => c.JerarquiasUoM).Returns(jerarquiasMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new CreateJerarquiaUoMCommandHandler(_dbContextMock.Object);
    }

    private void SeedExisting(Guid unidadMayorId, Guid unidadMenorId, decimal multiplicador) =>
        _jerarquiasList.Add(JerarquiaUoM.Create(_productoId, unidadMayorId, unidadMenorId, multiplicador).Value!);

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenProductoIsUnknownOrForeign()
    {
        var result = await _handler.Handle(new CreateJerarquiaUoMCommand(Guid.NewGuid(), _caja, _blister, 4m), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Medicamento.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = _jerarquiasList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenAUnitIsUnknownOrForeign(bool mayorDesconocida)
    {
        var command = mayorDesconocida
            ? new CreateJerarquiaUoMCommand(_productoId, Guid.NewGuid(), _blister, 4m)
            : new CreateJerarquiaUoMCommand(_productoId, _caja, Guid.NewGuid(), 4m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Error.Code.Should().Be("UnidadMedida.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = _jerarquiasList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenNoExistingConversionConnectsTheUnits()
    {
        var command = new CreateJerarquiaUoMCommand(_productoId, _caja, _blister, 4m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _jerarquiasList.Should().ContainSingle();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenRedundantEdgeAgreesWithTheResolvedChain()
    {
        // 1 Caja = 4 Blister, 1 Blister = 10 Unidad -> the chain already implies 1 Caja = 40 Unidad
        SeedExisting(_caja, _blister, 4m);
        SeedExisting(_blister, _unidad, 10m);

        var command = new CreateJerarquiaUoMCommand(_productoId, _caja, _unidad, 40m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _jerarquiasList.Should().HaveCount(3);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenRedundantEdgeContradictsTheResolvedChain()
    {
        SeedExisting(_caja, _blister, 4m);
        SeedExisting(_blister, _unidad, 10m);

        // The chain says 40, but this row claims 35 for the same product/units.
        var command = new CreateJerarquiaUoMCommand(_productoId, _caja, _unidad, 35m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("JerarquiaUoM.Multiplicador.Inconsistente");
        _ = result.StatusCode.Should().Be(409);
        _ = _jerarquiasList.Should().HaveCount(2);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldIgnoreConversionsFromOtherProducts_WhenCheckingConsistency()
    {
        var otroProductoId = Guid.NewGuid();
        _jerarquiasList.Add(JerarquiaUoM.Create(otroProductoId, _caja, _unidad, 999m).Value!);

        var command = new CreateJerarquiaUoMCommand(_productoId, _caja, _unidad, 40m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenTheSameConversionAlreadyExists()
    {
        SeedExisting(_caja, _blister, 4m);

        var result = await _handler.Handle(new CreateJerarquiaUoMCommand(_productoId, _caja, _blister, 4m), CancellationToken.None);

        _ = result.Error.Code.Should().Be("JerarquiaUoM.Duplicada");
        _ = result.StatusCode.Should().Be(409);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
