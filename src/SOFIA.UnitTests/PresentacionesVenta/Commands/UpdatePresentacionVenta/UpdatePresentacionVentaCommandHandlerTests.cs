using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.PresentacionesVenta.Commands.UpdatePresentacionVenta;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.PresentacionesVenta.Commands.UpdatePresentacionVenta;

public class UpdatePresentacionVentaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<PresentacionVenta> _presentacionesList = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<PresentacionVenta>> _presentacionesMock;
    private readonly UpdatePresentacionVentaCommandHandler _handler;

    private readonly Guid _productoId = Guid.NewGuid();
    private readonly Guid _unidadBase = Guid.NewGuid();
    private readonly Guid _caja = Guid.NewGuid();
    private readonly Guid _blister = Guid.NewGuid();
    private readonly Guid _mililitro = Guid.NewGuid();

    public UpdatePresentacionVentaCommandHandlerTests()
    {
        _presentacionesMock = _presentacionesList.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.PresentacionesVenta).Returns(_presentacionesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var producto = Medicamento.Create("COD1", "Paracetamol", Guid.NewGuid(), _unidadBase, CondicionVenta.VentaLibreOTC).Value!;
        producto.SetId(_productoId);
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { producto }.BuildMockDbSet().Object);

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
        var jerarquias = new List<JerarquiaUoM>
        {
            JerarquiaUoM.Create(_productoId, _caja, _blister, 4m).Value!,
            JerarquiaUoM.Create(_productoId, _blister, _unidadBase, 10m).Value!,
        };
        _ = _dbContextMock.Setup(c => c.JerarquiasUoM).Returns(jerarquias.BuildMockDbSet().Object);

        _handler = new UpdatePresentacionVentaCommandHandler(_dbContextMock.Object);
    }

    private void SetupFind(PresentacionVenta? presentacion) =>
        _presentacionesMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(presentacion));

    private PresentacionVenta CrearPresentacionCaja(decimal precio = 45m) =>
        PresentacionVenta.Create(_productoId, _caja, "Caja", 40m, precio).Value!;

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPresentacionDoesNotExist()
    {
        SetupFind(null);

        var result = await _handler.Handle(new UpdatePresentacionVentaCommand(Guid.NewGuid(), _caja, 45m), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndSave_WhenValid()
    {
        var presentacion = CrearPresentacionCaja();
        SetupFind(presentacion);

        var result = await _handler.Handle(new UpdatePresentacionVentaCommand(presentacion.Id, _caja, 80m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = presentacion.PrecioVenta.Should().Be(80m);
        _ = presentacion.CantidadUnidadesBase.Should().Be(40m);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRecomputeCantidad_WhenChangingToADifferentUnit()
    {
        var presentacion = CrearPresentacionCaja();
        SetupFind(presentacion);

        var result = await _handler.Handle(new UpdatePresentacionVentaCommand(presentacion.Id, _blister, 20m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = presentacion.UnidadVentaId.Should().Be(_blister);
        _ = presentacion.Descripcion.Should().Be("Blister");
        _ = presentacion.CantidadUnidadesBase.Should().Be(10m);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenAnotherPresentacionAlreadyUsesTheChosenUnit()
    {
        var presentacionCaja = CrearPresentacionCaja();
        var presentacionBlister = PresentacionVenta.Create(_productoId, _blister, "Blister", 10m, 20m).Value!;
        _presentacionesList.AddRange([presentacionCaja, presentacionBlister]);
        SetupFind(presentacionCaja);

        var result = await _handler.Handle(new UpdatePresentacionVentaCommand(presentacionCaja.Id, _blister, 20m), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.UnidadVenta.Duplicada");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenChosenUnitHasNoJerarquiaPathToBaseUnit()
    {
        var presentacion = CrearPresentacionCaja();
        SetupFind(presentacion);

        // Mililitro is a real, registered unit, but this product's Jerarquía never connects it to the base unit.
        var result = await _handler.Handle(new UpdatePresentacionVentaCommand(presentacion.Id, _mililitro, 45m), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.Jerarquia.NoResuelta");
    }
}
