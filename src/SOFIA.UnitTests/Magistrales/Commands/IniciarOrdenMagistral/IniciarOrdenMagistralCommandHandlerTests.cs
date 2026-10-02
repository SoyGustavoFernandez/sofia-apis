using System.Reflection;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Magistrales.Commands.IniciarOrdenMagistral;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Magistrales.Commands.IniciarOrdenMagistral;

public class IniciarOrdenMagistralCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _quimicoId = Guid.NewGuid();
    private readonly Medicamento _producto = Medicamento.Create("MAG-001", "Crema magistral", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
    private readonly List<InventarioSucursal> _inventarios = [];
    private readonly List<MagistralOrdenProduccion> _ordenes = [];
    private readonly IniciarOrdenMagistralCommandHandler _handler;

    public IniciarOrdenMagistralCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns(_sucursalId.ToString());
        _ = _currentUserMock.Setup(u => u.Id).Returns(_quimicoId.ToString());

        _ = _dbContextMock.Setup(db => db.Medicamentos).Returns(new List<Medicamento> { _producto }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(db => db.Recetas).Returns(new List<RecetaMedica>().BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(db => db.LotesEnSucursal).Returns(_inventarios.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(db => db.MagistralesConsumosInsumo).Returns(new List<MagistralConsumoInsumo>().BuildMockDbSet().Object);
        var ordenesDbSet = _ordenes.BuildMockDbSet();
        _ = ordenesDbSet.Setup(d => d.Add(It.IsAny<MagistralOrdenProduccion>())).Callback<MagistralOrdenProduccion>(_ordenes.Add);
        _ = _dbContextMock.Setup(db => db.MagistralesOrdenesProduccion).Returns(ordenesDbSet.Object);

        _handler = new IniciarOrdenMagistralCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    // Expiry stored like the SPA sends it (-05:00) for an expiry day that started the given hours ago.
    private static DateTimeOffset CaducidadIniciadaHace(double horas) => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).AddHours(-horas);

    // Builds the stock row with its Lote navigation loaded, as the handler's Include does
    private InventarioSucursal AddInventario(Guid sucursalId, decimal cantidad, DateTimeOffset? caducidad = null, bool conLote = true)
    {
        var lote = LoteInventario.Create(Guid.NewGuid(), "L-INSUMO", null, DateTimeOffset.UtcNow.AddYears(1)).Value!;
        if (caducidad.HasValue)
        {
            typeof(LoteInventario).GetProperty(nameof(LoteInventario.FechaCaducidad))!.SetValue(lote, caducidad.Value);
        }

        var inventario = InventarioSucursal.Create(sucursalId, lote.Id, cantidad).Value!;
        if (conLote)
        {
            typeof(InventarioSucursal).GetField("<Lote>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(inventario, lote);
        }

        _inventarios.Add(inventario);
        return inventario;
    }

    private IniciarOrdenMagistralCommand Command(Guid inventarioId, decimal cantidad, Guid? recetaId = null) =>
        new(recetaId, _producto.Id, 10, [new(inventarioId, cantidad)]);

    [Theory]
    [InlineData(24)]
    [InlineData(36)]
    public async Task Handle_ShouldReturnLoteVencido_WhenTheInputExpiryDayAlreadyEnded(double horas)
    {
        var inventario = AddInventario(_sucursalId, 50m, CaducidadIniciadaHace(horas));

        var result = await _handler.Handle(Command(inventario.Id, 10), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Magistral.Insumo.LoteVencido");
        _ = inventario.CantidadFisica.Should().Be(50m, because: "an expired input must never be consumed");
        _ = _ordenes.Should().BeEmpty();
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        _dbContextMock.Verify(db => db.MagistralesConsumosInsumo.Add(It.IsAny<MagistralConsumoInsumo>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(23)]
    public async Task Handle_ShouldCreateOrder_WhenTheInputLotExpiresToday(double horas)
    {
        var inventario = AddInventario(_sucursalId, 50m, CaducidadIniciadaHace(horas));

        var result = await _handler.Handle(Command(inventario.Id, 10), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = inventario.CantidadFisica.Should().Be(40m);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotSaveAnything_WhenALaterInputIsExpired()
    {
        var vigente = AddInventario(_sucursalId, 50m);
        var vencido = AddInventario(_sucursalId, 30m, CaducidadIniciadaHace(36));
        var command = new IniciarOrdenMagistralCommand(null, _producto.Id, 10, [new(vigente.Id, 10), new(vencido.Id, 5)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Magistral.Insumo.LoteVencido");
        _ = vencido.CantidadFisica.Should().Be(30m);
        _ = _ordenes.Should().BeEmpty();
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never, "the deduction of the first input must not be persisted");
    }

    [Fact]
    public async Task Handle_ShouldReturnLoteVencido_WhenTheInputLotIsNotLoaded()
    {
        var inventario = AddInventario(_sucursalId, 50m, conLote: false);

        var result = await _handler.Handle(Command(inventario.Id, 10), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Magistral.Insumo.LoteVencido");
        _ = inventario.CantidadFisica.Should().Be(50m);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreateOrderForSessionChemistAndBranch_WhenStockIsAvailable()
    {
        var inventario = AddInventario(_sucursalId, 50m);

        var result = await _handler.Handle(Command(inventario.Id, 10), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = inventario.CantidadFisica.Should().Be(40m);
        var orden = _ordenes.Should().ContainSingle().Subject;
        _ = orden.SucursalId.Should().Be(_sucursalId);
        _ = orden.QuimicoPreparadorId.Should().Be(_quimicoId);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(db => db.MagistralesConsumosInsumo.Add(It.IsAny<MagistralConsumoInsumo>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenStockBelongsToAnotherBranch()
    {
        var inventarioAjeno = AddInventario(Guid.NewGuid(), 50m);

        var result = await _handler.Handle(Command(inventarioAjeno.Id, 10), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Inventario.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = inventarioAjeno.CantidadFisica.Should().Be(50m, because: "another branch's stock must never be consumed");
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailure_WhenStockIsInsufficient()
    {
        var inventario = AddInventario(_sucursalId, 5m);

        var result = await _handler.Handle(Command(inventario.Id, 10), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Inventario.StockInsuficiente");
        _ = inventario.CantidadFisica.Should().Be(5m);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenInventoryDoesNotExist()
    {
        var result = await _handler.Handle(Command(Guid.NewGuid(), 10), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Inventario.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(db => db.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductIsNotInCompany()
    {
        var inventario = AddInventario(_sucursalId, 50m);

        var result = await _handler.Handle(Command(inventario.Id, 10) with { ProductoResultanteId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(404);
        _ = result.Error.Code.Should().Be("Medicamento.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPrescriptionIsNotInCompany()
    {
        var inventario = AddInventario(_sucursalId, 50m);

        var result = await _handler.Handle(Command(inventario.Id, 10, recetaId: Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("RecetaMedica.NotFound");
    }
}
