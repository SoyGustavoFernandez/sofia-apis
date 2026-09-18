using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.PresentacionesVenta.Domain;

public class PresentacionVentaTests
{
    private static readonly Guid ProductoId = Guid.NewGuid();
    private static readonly Guid UnidadVentaId = Guid.NewGuid();

    private static PresentacionVenta CrearPresentacion() =>
        PresentacionVenta.Create(ProductoId, UnidadVentaId, "Caja", 10m, 45m).Value!;

    [Fact]
    public void Create_ShouldSucceed_WhenAllFieldsAreValid()
    {
        var result = PresentacionVenta.Create(ProductoId, UnidadVentaId, "Caja", 10m, 45m);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.UnidadVentaId.Should().Be(UnidadVentaId);
        _ = result.Value!.Descripcion.Should().Be("Caja");
        _ = result.Value!.CantidadUnidadesBase.Should().Be(10m);
        _ = result.Value!.PrecioVenta.Should().Be(45m);
    }

    [Fact]
    public void Create_ShouldFail_WhenProductoIdIsEmpty()
    {
        var result = PresentacionVenta.Create(Guid.Empty, UnidadVentaId, "Caja", 10m, 45m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.ProductoId");
    }

    [Fact]
    public void Create_ShouldFail_WhenUnidadVentaIdIsEmpty()
    {
        var result = PresentacionVenta.Create(ProductoId, Guid.Empty, "Caja", 10m, 45m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.UnidadVentaId");
    }

    [Fact]
    public void Create_ShouldFail_WhenDescripcionIsEmpty()
    {
        var result = PresentacionVenta.Create(ProductoId, UnidadVentaId, "", 10m, 45m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.Descripcion");
    }

    [Fact]
    public void Create_ShouldFail_WhenDescripcionExceedsMaxLength()
    {
        var result = PresentacionVenta.Create(ProductoId, UnidadVentaId, new string('a', 101), 10m, 45m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.Descripcion");
    }

    [Fact]
    public void Create_ShouldFail_WhenCantidadUnidadesBaseIsZeroOrNegative()
    {
        var result = PresentacionVenta.Create(ProductoId, UnidadVentaId, "Caja", 0m, 45m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.CantidadUnidadesBase");
    }

    [Fact]
    public void Create_ShouldFail_WhenPrecioVentaIsNegative()
    {
        var result = PresentacionVenta.Create(ProductoId, UnidadVentaId, "Caja", 10m, -1m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.PrecioVenta");
    }

    [Fact]
    public void Create_ShouldSucceed_WhenPrecioVentaIsZero()
    {
        var result = PresentacionVenta.Create(ProductoId, UnidadVentaId, "Promo", 1m, 0m);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldChangeFields_WhenValid()
    {
        var presentacion = CrearPresentacion();
        var nuevaUnidad = Guid.NewGuid();

        var result = presentacion.Update(nuevaUnidad, "Caja x20", 20m, 80m);

        _ = result.IsSuccess.Should().BeTrue();
        _ = presentacion.UnidadVentaId.Should().Be(nuevaUnidad);
        _ = presentacion.Descripcion.Should().Be("Caja x20");
        _ = presentacion.CantidadUnidadesBase.Should().Be(20m);
        _ = presentacion.PrecioVenta.Should().Be(80m);
    }

    [Fact]
    public void Update_ShouldFail_WhenUnidadVentaIdIsEmpty()
    {
        var presentacion = CrearPresentacion();

        var result = presentacion.Update(Guid.Empty, "Caja x20", 20m, 80m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.UnidadVentaId");
    }

    [Fact]
    public void Update_ShouldFail_WhenDescripcionIsEmpty()
    {
        var presentacion = CrearPresentacion();

        var result = presentacion.Update(UnidadVentaId, "", 20m, 80m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.Descripcion");
    }

    [Fact]
    public void Update_ShouldFail_WhenCantidadUnidadesBaseIsZeroOrNegative()
    {
        var presentacion = CrearPresentacion();

        var result = presentacion.Update(UnidadVentaId, "Caja x20", -5m, 80m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.CantidadUnidadesBase");
    }

    [Fact]
    public void Update_ShouldFail_WhenPrecioVentaIsNegative()
    {
        var presentacion = CrearPresentacion();

        var result = presentacion.Update(UnidadVentaId, "Caja x20", 20m, -1m);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PresentacionVenta.PrecioVenta");
    }
}
