using FluentAssertions;
using Moq;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Common.Extensions;

public class CurrentUserExtensionsTests
{
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _sucursalId = Guid.NewGuid();

    public CurrentUserExtensionsTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns(_sucursalId.ToString());
    }

    [Fact]
    public void GetSucursalScope_ShouldReturnTheSessionBranch_WhenUserIsNotAdmin()
    {
        var result = _currentUserMock.Object.GetSucursalScope();

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(_sucursalId);
    }

    [Fact]
    public void GetSucursalScope_ShouldReturnNoRestriction_WhenUserIsAdmin()
    {
        _ = _currentUserMock.Setup(u => u.IsInRole(Rol.AdminRoleName)).Returns(true);

        var result = _currentUserMock.Object.GetSucursalScope();

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().BeNull();
    }

    [Fact]
    public void GetSucursalScope_ShouldFail_WhenNonAdminHasNoBranch()
    {
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns((string?)null);

        var result = _currentUserMock.Object.GetSucursalScope();

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Auth.Sucursal");
    }
}
