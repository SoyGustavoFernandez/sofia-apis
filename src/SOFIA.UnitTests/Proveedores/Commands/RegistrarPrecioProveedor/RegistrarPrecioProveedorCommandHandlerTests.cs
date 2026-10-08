using System.Globalization;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Proveedores.Commands.RegistrarPrecioProveedor;

public class RegistrarPrecioProveedorCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<HistorialPrecioProveedor> _precios = [];
    private readonly ProveedorDistribuidor _proveedor = ProveedorDistribuidor.Create("Distribuidora SAC", "20100000001", null, null).Value!;
    private readonly ProveedorDistribuidor _otroProveedor = ProveedorDistribuidor.Create("Otra SAC", "20100000002", null, null).Value!;
    private readonly Medicamento _medicamento = Medicamento.Create("COD1", "Paracetamol", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
    private readonly RegistrarPrecioProveedorCommandHandler _handler;

    public RegistrarPrecioProveedorCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Proveedores).Returns(new List<ProveedorDistribuidor> { _proveedor, _otroProveedor }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { _medicamento }.BuildMockDbSet().Object);

        var preciosMock = _precios.BuildMockDbSet();
        _ = preciosMock.Setup(d => d.Add(It.IsAny<HistorialPrecioProveedor>())).Callback<HistorialPrecioProveedor>(_precios.Add);
        _ = _dbContextMock.Setup(c => c.HistorialPreciosProveedor).Returns(preciosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new RegistrarPrecioProveedorCommandHandler(_dbContextMock.Object);
    }

    private RegistrarPrecioProveedorCommand Command(decimal precio = 10m, decimal descuento = 15m) => new(_proveedor.Id, _medicamento.Id, precio, descuento);

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTheProveedorDoesNotExist()
    {
        var result = await _handler.Handle(Command() with { ProveedorId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Proveedor.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = _precios.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenTheMedicamentoDoesNotExist()
    {
        var result = await _handler.Handle(Command() with { MedicamentoId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Medicamento.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = _precios.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAddAnOpenPriceWithTheDiscountApplied_WhenValid()
    {
        var antes = DateTime.UtcNow;

        var result = await _handler.Handle(Command(10m, 15m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        var precio = _precios.Should().ContainSingle().Subject;
        _ = result.Value.Should().Be(precio.Id);
        _ = precio.ProveedorId.Should().Be(_proveedor.Id);
        _ = precio.ProductoId.Should().Be(_medicamento.Id);
        _ = precio.CostoPorUnidadBase.Should().Be(8.5m);
        _ = precio.FechaFinVigencia.Should().BeNull();
        _ = precio.FechaInicioVigencia.Should().BeOnOrAfter(antes);
        _ = precio.LeadTimeDias.Should().Be(0);
        _ = precio.CantidadMinCompra.Should().Be(1);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData("10", "0", "10")]
    [InlineData("10", "100", "0")]
    [InlineData("3.3333", "33.33", "2.2223")]
    [InlineData("1", "33.3333", "0.6667")]
    public async Task Handle_ShouldRoundTheNetCostToFourDecimals(string precio, string descuento, string esperado)
    {
        _ = await _handler.Handle(Command(decimal.Parse(precio, CultureInfo.InvariantCulture), decimal.Parse(descuento, CultureInfo.InvariantCulture)), CancellationToken.None);

        _ = _precios.Single().CostoPorUnidadBase.Should().Be(decimal.Parse(esperado, CultureInfo.InvariantCulture));
    }

    [Fact]
    public async Task Handle_ShouldCloseThePreviousOpenPriceOfTheSameProveedorAndProducto()
    {
        var anterior = HistorialPrecioProveedor.Create(_proveedor.Id, _medicamento.Id, 9m, DateTime.UtcNow.AddDays(-10), null, 0).Value!;
        _precios.Add(anterior);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        var nuevo = _precios.Single(p => p.Id == result.Value);
        _ = anterior.FechaFinVigencia.Should().Be(nuevo.FechaInicioVigencia);
        _ = nuevo.FechaFinVigencia.Should().BeNull();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldLeaveUntouchedThePricesOfOtherProveedoresAndAlreadyClosedOnes()
    {
        var deOtroProveedor = HistorialPrecioProveedor.Create(_otroProveedor.Id, _medicamento.Id, 7m, DateTime.UtcNow.AddDays(-10), null, 0).Value!;
        var cerrado = HistorialPrecioProveedor.Create(_proveedor.Id, _medicamento.Id, 6m, DateTime.UtcNow.AddDays(-20), DateTime.UtcNow.AddDays(-15), 0).Value!;
        var cerradoEn = cerrado.FechaFinVigencia;
        _precios.AddRange([deOtroProveedor, cerrado]);

        _ = await _handler.Handle(Command(), CancellationToken.None);

        _ = deOtroProveedor.FechaFinVigencia.Should().BeNull();
        _ = cerrado.FechaFinVigencia.Should().Be(cerradoEn);
    }

    [Fact]
    public async Task Handle_ShouldLeaveUntouchedTheOpenPriceOfAnotherProducto()
    {
        var deOtroProducto = HistorialPrecioProveedor.Create(_proveedor.Id, Guid.NewGuid(), 5m, DateTime.UtcNow.AddDays(-10), null, 0).Value!;
        _precios.Add(deOtroProducto);

        _ = await _handler.Handle(Command(), CancellationToken.None);

        _ = deOtroProducto.FechaFinVigencia.Should().BeNull();
    }
}
