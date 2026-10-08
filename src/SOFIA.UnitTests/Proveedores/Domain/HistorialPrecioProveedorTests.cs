using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Proveedores.Domain;

public class HistorialPrecioProveedorTests
{
    private static readonly DateTime Inicio = new(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc);

    private static HistorialPrecioProveedor NuevoPrecioAbierto() =>
        HistorialPrecioProveedor.Create(Guid.NewGuid(), Guid.NewGuid(), 8.5m, Inicio, null, 0).Value!;

    [Fact]
    public void Create_ShouldLeaveThePriceOpen_WhenNoEndDateIsGiven()
    {
        var result = HistorialPrecioProveedor.Create(Guid.NewGuid(), Guid.NewGuid(), 8.5m, Inicio, null, 0);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.FechaFinVigencia.Should().BeNull();
        _ = result.Value!.CantidadMinCompra.Should().Be(1);
    }

    [Fact]
    public void CerrarVigencia_ShouldSetTheEndDate_WhenItIsAfterTheStart()
    {
        var precio = NuevoPrecioAbierto();
        var fin = Inicio.AddDays(3);

        var result = precio.CerrarVigencia(fin);

        _ = result.IsSuccess.Should().BeTrue();
        _ = precio.FechaFinVigencia.Should().Be(fin);
    }

    [Fact]
    public void CerrarVigencia_ShouldAcceptTheSameInstantAsTheStart()
    {
        var precio = NuevoPrecioAbierto();

        var result = precio.CerrarVigencia(Inicio);

        _ = result.IsSuccess.Should().BeTrue();
        _ = precio.FechaFinVigencia.Should().Be(Inicio);
    }

    [Fact]
    public void CerrarVigencia_ShouldFailAndKeepThePriceOpen_WhenTheEndIsBeforeTheStart()
    {
        var precio = NuevoPrecioAbierto();

        var result = precio.CerrarVigencia(Inicio.AddSeconds(-1));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("HistorialPrecioProveedor.FechaFinVigencia");
        _ = precio.FechaFinVigencia.Should().BeNull();
    }
}
