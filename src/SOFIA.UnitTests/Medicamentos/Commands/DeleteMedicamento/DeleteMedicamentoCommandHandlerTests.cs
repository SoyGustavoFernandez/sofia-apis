using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Medicamentos.Commands.DeleteMedicamento;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Medicamentos.Commands.DeleteMedicamento;

public class DeleteMedicamentoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<DbSet<Medicamento>> _medicamentosDbSetMock = new();
    private readonly Medicamento _medicamento = Medicamento.Create("COD-001", "Paracetamol 500mg", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
    private readonly DeleteMedicamentoCommandHandler _handler;

    public DeleteMedicamentoCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(_medicamentosDbSetMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _medicamentosDbSetMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Medicamento?>(_medicamento));
        SetupDependents();
        _handler = new DeleteMedicamentoCommandHandler(_dbContextMock.Object);
    }

    private void SetupDependents(
        LoteInventario? lote = null,
        PresentacionVenta? presentacion = null,
        JerarquiaUoM? jerarquia = null,
        FormulacionClinica? formulacion = null,
        HistorialPrecioProveedor? precio = null,
        ServicioAgenda? cita = null,
        ServicioClinicoInmunizacion? inmunizacion = null,
        MagistralOrdenProduccion? orden = null)
    {
        _ = _dbContextMock.Setup(c => c.LotesInventario).Returns(Of(lote).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.PresentacionesVenta).Returns(Of(presentacion).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.JerarquiasUoM).Returns(Of(jerarquia).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.FormulacionesClinicas).Returns(Of(formulacion).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.HistorialPreciosProveedor).Returns(Of(precio).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.ServiciosAgenda).Returns(Of(cita).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.ServiciosClinicosInmunizacion).Returns(Of(inmunizacion).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.MagistralesOrdenesProduccion).Returns(Of(orden).BuildMockDbSet().Object);
    }

    private static List<T> Of<T>(T? item) where T : class => item is null ? [] : [item];

    private LoteInventario NewLote() => LoteInventario.Create(_medicamento.Id, "L-001", null, DateTimeOffset.UtcNow.AddYears(1)).Value!;

    private PresentacionVenta NewPresentacion() => PresentacionVenta.Create(_medicamento.Id, Guid.NewGuid(), "Caja", 10m, 20m).Value!;

    private JerarquiaUoM NewJerarquia() => JerarquiaUoM.Create(_medicamento.Id, Guid.NewGuid(), Guid.NewGuid(), 10m).Value!;

    private FormulacionClinica NewFormulacion() => FormulacionClinica.Create(_medicamento.Id, Guid.NewGuid(), 500m, Guid.NewGuid(), null).Value!;

    private HistorialPrecioProveedor NewPrecio() => HistorialPrecioProveedor.Create(Guid.NewGuid(), _medicamento.Id, 5m, DateTime.UtcNow, null, 3).Value!;

    private ServicioAgenda NewCita() => ServicioAgenda.Create(Guid.NewGuid(), _medicamento.Id, null, DateTime.UtcNow.AddDays(1)).Value!;

    private ServicioClinicoInmunizacion NewInmunizacion() =>
        ServicioClinicoInmunizacion.Create(null, Guid.NewGuid(), Guid.NewGuid(), _medicamento.Id, Guid.NewGuid(), "IM", "Brazo", 0.5m, DateTime.UtcNow, null, ModalidadRegistro.Corriente).Value!;

    private MagistralOrdenProduccion NewOrden() =>
        MagistralOrdenProduccion.Create(Guid.NewGuid(), null, _medicamento.Id, null, null, Guid.NewGuid(), MagistralOrdenProduccion.EstadoIniciada).Value!;

    private async Task AssertConflictAsync()
    {
        var result = await _handler.Handle(new DeleteMedicamentoCommand(_medicamento.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.InUse");
        _ = result.StatusCode.Should().Be(409);
        _medicamentosDbSetMock.Verify(m => m.Remove(It.IsAny<Medicamento>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenMedicamentoDoesNotExist()
    {
        _ = _medicamentosDbSetMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult<Medicamento?>(null));

        var result = await _handler.Handle(new DeleteMedicamentoCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByLote()
    {
        SetupDependents(lote: NewLote());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByPresentacion()
    {
        SetupDependents(presentacion: NewPresentacion());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByJerarquia()
    {
        SetupDependents(jerarquia: NewJerarquia());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByFormulacion()
    {
        SetupDependents(formulacion: NewFormulacion());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByPrecioProveedor()
    {
        SetupDependents(precio: NewPrecio());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByCita()
    {
        SetupDependents(cita: NewCita());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByInmunizacion()
    {
        SetupDependents(inmunizacion: NewInmunizacion());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByOrdenMagistral()
    {
        SetupDependents(orden: NewOrden());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenAllDependentsAreSoftDeleted()
    {
        var lote = NewLote();
        var presentacion = NewPresentacion();
        var jerarquia = NewJerarquia();
        var formulacion = NewFormulacion();
        var precio = NewPrecio();
        var cita = NewCita();
        var inmunizacion = NewInmunizacion();
        var orden = NewOrden();
        lote.IsDeleted = presentacion.IsDeleted = jerarquia.IsDeleted = formulacion.IsDeleted = true;
        precio.IsDeleted = cita.IsDeleted = inmunizacion.IsDeleted = orden.IsDeleted = true;
        SetupDependents(lote, presentacion, jerarquia, formulacion, precio, cita, inmunizacion, orden);

        var result = await _handler.Handle(new DeleteMedicamentoCommand(_medicamento.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _medicamentosDbSetMock.Verify(m => m.Remove(_medicamento), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenMedicamentoHasNoDependents()
    {
        var result = await _handler.Handle(new DeleteMedicamentoCommand(_medicamento.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _medicamentosDbSetMock.Verify(m => m.Remove(_medicamento), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
