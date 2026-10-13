using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Sucursales.Domain;

public class SucursalTests
{
    [Fact]
    public void CreateSedePrincipal_ShouldLeaveLicenceEmpty_WhenDataIsValid()
    {
        var empresaId = Guid.NewGuid();

        var result = Sucursal.CreateSedePrincipal("Sede Principal", "Av. Los Olivos 123, Lima", empresaId);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Nombre.Should().Be("Sede Principal");
        _ = result.Value.Direccion_Fisica.Should().Be("Av. Los Olivos 123, Lima");
        _ = result.Value.Numero_Licencia.Should().BeEmpty();
        _ = result.Value.EmpresaId.Should().Be(empresaId);
        _ = result.Value.TenantId.Should().Be(empresaId);
    }

    [Theory]
    [InlineData("", "Av. Los Olivos 123", "Sucursal.Nombre")]
    [InlineData("   ", "Av. Los Olivos 123", "Sucursal.Nombre")]
    [InlineData("Sede Principal", "", "Sucursal.Direccion")]
    [InlineData("Sede Principal", "   ", "Sucursal.Direccion")]
    public void CreateSedePrincipal_ShouldFail_WhenNameOrAddressIsBlank(string nombre, string direccion, string codigo)
    {
        var result = Sucursal.CreateSedePrincipal(nombre, direccion, Guid.NewGuid());

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be(codigo);
    }

    [Fact]
    public void Update_ShouldRequireLicence_WhenMainBranchWasCreatedWithoutIt()
    {
        var sucursal = Sucursal.CreateSedePrincipal("Sede Principal", "Av. Los Olivos 123, Lima", Guid.NewGuid()).Value!;

        var sinLicencia = sucursal.Update("Sede Principal", "Av. Los Olivos 123, Lima", "", null);
        var conLicencia = sucursal.Update("Sede Principal", "Av. Los Olivos 123, Lima", "LIC-0001", null);

        _ = sinLicencia.IsFailure.Should().BeTrue();
        _ = sinLicencia.Error.Code.Should().Be("Sucursal.Licencia");
        _ = conLicencia.IsSuccess.Should().BeTrue();
        _ = sucursal.Numero_Licencia.Should().Be("LIC-0001");
    }
}
