using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Inventarios.Domain;

public class InventarioSucursalTests
{
    private static InventarioSucursal NewEntry(decimal cantidad = 50) =>
        InventarioSucursal.Create(Guid.NewGuid(), Guid.NewGuid(), cantidad).Value!;

    [Fact]
    public void Create_ShouldFail_WhenQuantityIsNegative()
    {
        var result = InventarioSucursal.Create(Guid.NewGuid(), Guid.NewGuid(), -1);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("InventarioSucursal.CantidadFisica");
    }

    [Fact]
    public void AdjustStock_ShouldSetNewQuantity_WhenValueIsValid()
    {
        var entry = NewEntry(50);

        var result = entry.AdjustStock(30);

        _ = result.IsSuccess.Should().BeTrue();
        _ = entry.CantidadFisica.Should().Be(30);
    }

    [Fact]
    public void AdjustStock_ShouldAllowZero()
    {
        var entry = NewEntry(50);

        var result = entry.AdjustStock(0);

        _ = result.IsSuccess.Should().BeTrue();
        _ = entry.CantidadFisica.Should().Be(0);
    }

    [Fact]
    public void AdjustStock_ShouldFail_WhenQuantityIsNegative()
    {
        var entry = NewEntry(50);

        var result = entry.AdjustStock(-5);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("InventarioSucursal.CantidadFisica");
        _ = entry.CantidadFisica.Should().Be(50);
    }
}
