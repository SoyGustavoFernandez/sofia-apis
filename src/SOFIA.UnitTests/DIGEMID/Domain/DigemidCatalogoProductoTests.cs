using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.DIGEMID.Domain;

public class DigemidCatalogoProductoTests
{
    [Fact]
    public void Create_ShouldBeGlobal_WhenNoTenantIsGiven()
    {
        var producto = DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo").Value!;

        _ = producto.IsGlobal.Should().BeTrue();
        _ = producto.TenantId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldNotBeGlobal_WhenATenantIsGiven()
    {
        var tenant = Guid.NewGuid();

        var producto = DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo", tenant).Value!;

        _ = producto.IsGlobal.Should().BeFalse();
        _ = producto.TenantId.Should().Be(tenant);
    }

    [Fact]
    public void Update_ShouldFailAndKeepValues_WhenProductIsGlobal()
    {
        var producto = DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo").Value!;

        var result = producto.Update("K-001", "Otro nombre", null, null, null, null, null, "Activo");

        _ = result.Error.Should().Be(DigemidCatalogoProducto.GlobalReadOnlyError);
        _ = producto.NomProd.Should().Be("Paracetamol");
    }

    [Fact]
    public void Update_ShouldSucceed_WhenProductBelongsToATenant()
    {
        var producto = DigemidCatalogoProducto.Create("K-001", "Paracetamol", null, null, null, null, null, "Activo", Guid.NewGuid()).Value!;

        var result = producto.Update("K-001", "Otro nombre", null, null, null, null, null, "Activo");

        _ = result.IsSuccess.Should().BeTrue();
        _ = producto.NomProd.Should().Be("Otro nombre");
    }
}
