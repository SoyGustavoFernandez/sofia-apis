using FluentAssertions;
using SOFIA.Application.Dashboard.Common;

namespace SOFIA.UnitTests.Dashboard.Common;

public class DashboardScopeTests
{
    private static readonly DashboardSucursal Norte = new(Guid.NewGuid(), "Norte");
    private static readonly DashboardSucursal Sur = new(Guid.NewGuid(), "Sur");

    [Fact]
    public void Ids_ShouldListTheBranchIdsInOrder() =>
        _ = new DashboardScope([Norte, Sur], true).Ids.Should().Equal(Norte.Id, Sur.Id);

    [Fact]
    public void MultiSucursal_ShouldBeTrue_WhenTheScopeHasMoreThanOneBranch() =>
        _ = new DashboardScope([Norte, Sur], false).MultiSucursal.Should().BeTrue();

    [Fact]
    public void MultiSucursal_ShouldBeFalse_WhenTheWholeCompanyIsASingleBranch()
    {
        var scope = new DashboardScope([Norte], true);

        _ = scope.MultiSucursal.Should().BeFalse();
        _ = scope.EmpresaCompleta.Should().BeTrue();
    }

    [Fact]
    public void Ids_ShouldBeEmpty_WhenTheScopeHasNoBranches()
    {
        var scope = new DashboardScope([], false);

        _ = scope.Ids.Should().BeEmpty();
        _ = scope.MultiSucursal.Should().BeFalse();
    }
}
