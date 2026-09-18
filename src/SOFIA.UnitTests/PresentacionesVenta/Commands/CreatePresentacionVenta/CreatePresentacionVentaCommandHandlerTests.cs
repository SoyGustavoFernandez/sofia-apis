using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.PresentacionesVenta.Commands.CreatePresentacionVenta;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.PresentacionesVenta.Commands.CreatePresentacionVenta;

public class CreatePresentacionVentaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<PresentacionVenta> _presentacionesList = [];
    private readonly List<Medicamento> _productosList = [];
    private readonly List<JerarquiaUoM> _jerarquiasList = [];
    private readonly CreatePresentacionVentaCommandHandler _handler;

    private readonly Guid _productoId = Guid.NewGuid();
    private readonly Guid _unidadBase = Guid.NewGuid();
    private readonly Guid _caja = Guid.NewGuid();
    private readonly Guid _blister = Guid.NewGuid();
    private readonly Guid _mililitro = Guid.NewGuid();

    public CreatePresentacionVentaCommandHandlerTests()
    {
        var presentacionesMock = _presentacionesList.BuildMockDbSet();
        _ = presentacionesMock.Setup(d => d.Add(It.IsAny<PresentacionVenta>())).Callback<PresentacionVenta>(_presentacionesList.Add);
        _ = _dbContextMock.Setup(c => c.PresentacionesVenta).Returns(presentacionesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var producto = Medicamento.Create("COD1", "Paracetamol", Guid.NewGuid(), _unidadBase, CondicionVenta.VentaLibreOTC).Value!;
        producto.SetId(_productoId);
        _productosList.Add(producto);
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(_productosList.BuildMockDbSet().Object);

        var unidadBase = UnidadMedida.Create("UN", "Unidad").Value!;
        unidadBase.SetId(_unidadBase);
        var caja = UnidadMedida.Create("CJ", "Caja").Value!;
        caja.SetId(_caja);
        var blister = UnidadMedida.Create("BL", "Blister").Value!;
        blister.SetId(_blister);
        var mililitro = UnidadMedida.Create("ML", "Mililitro").Value!;
        mililitro.SetId(_mililitro);
        _ = _dbContextMock.Setup(c => c.UnidadesMedida).Returns(new List<UnidadMedida> { unidadBase, caja, blister, mililitro }.BuildMockDbSet().Object);

        // 1 Caja = 4 Blister, 1 Blister = 10 Unidad -> 1 Caja = 40 Unidad
        _jerarquiasList.Add(JerarquiaUoM.Create(_productoId, _caja, _blister, 4m).Value!);
        _jerarquiasList.Add(JerarquiaUoM.Create(_productoId, _blister, _unidadBase, 10m).Value!);
        _ = _dbContextMock.Setup(c => c.JerarquiasUoM).Returns(_jerarquiasList.BuildMockDbSet().Object);

        _handler = new CreatePresentacionVentaCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreatePresentacion_WhenValid()
    {
        var command = new CreatePresentacionVentaCommand(_productoId, _caja, 45m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(201);
        _ = _presentacionesList.Should().ContainSingle();
        _ = _presentacionesList[0].Descripcion.Should().Be("Caja");
        _ = _presentacionesList[0].CantidadUnidadesBase.Should().Be(40m);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldResolveMultiHopChain_WhenSellingByAnIntermediateUnit()
    {
        var command = new CreatePresentacionVentaCommand(_productoId, _blister, 20m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _presentacionesList[0].CantidadUnidadesBase.Should().Be(10m);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductoDoesNotExist()
    {
        var command = new CreatePresentacionVentaCommand(Guid.NewGuid(), _caja, 45m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.Producto");
        _ = _presentacionesList.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenAPresentacionAlreadyExistsForThatUnit()
    {
        _presentacionesList.Add(PresentacionVenta.Create(_productoId, _caja, "Caja", 40m, 45m).Value!);

        var command = new CreatePresentacionVentaCommand(_productoId, _caja, 50m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.UnidadVenta.Duplicada");
        _ = result.StatusCode.Should().Be(409);
        _ = _presentacionesList.Should().HaveCount(1);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenNoJerarquiaConnectsTheChosenUnitToTheBaseUnit()
    {
        // Mililitro is a real, registered unit, but this product's Jerarquía never connects it to the base unit.
        var command = new CreatePresentacionVentaCommand(_productoId, _mililitro, 45m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.Jerarquia.NoResuelta");
        _ = _presentacionesList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
