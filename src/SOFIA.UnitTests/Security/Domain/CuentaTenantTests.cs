using FluentAssertions;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Domain;

public class CuentaTenantTests
{
    [Fact]
    public void SucursalBasePerteneceAlTenant_SameCompany_ReturnsTrue()
    {
        var tenantId = Guid.NewGuid();

        var cuenta = CuentaFactory.WithBaseBranch(tenantId, tenantId);

        _ = cuenta.SucursalBasePerteneceAlTenant.Should().BeTrue();
    }

    [Fact]
    public void SucursalBasePerteneceAlTenant_BranchOfAnotherCompany_ReturnsFalse()
    {
        var cuenta = CuentaFactory.WithBaseBranch(Guid.NewGuid(), Guid.NewGuid());

        _ = cuenta.SucursalBasePerteneceAlTenant.Should().BeFalse();
    }

    [Fact]
    public void SucursalBasePerteneceAlTenant_AccountWithoutTenant_ReturnsFalse()
    {
        var cuenta = CuentaFactory.WithBaseBranch(null, null);

        _ = cuenta.SucursalBasePerteneceAlTenant.Should().BeFalse();
    }

    [Fact]
    public void SucursalBasePerteneceAlTenant_EmpleadoNotLoaded_ReturnsFalse()
    {
        var cuenta = Cuenta.Create(Guid.NewGuid(), "usuario", "hash", Guid.NewGuid()).Value!;

        _ = cuenta.SucursalBasePerteneceAlTenant.Should().BeFalse();
    }
}
