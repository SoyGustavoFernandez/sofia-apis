using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Roles.RevokePermission;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Roles.RevokePermission;

public class RevokePermissionFromRolCommandHandlerTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();

    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IPermissionCache> _permissionCacheMock = new();
    private readonly Rol _rolCajero = Rol.Create("Cajero", null).Value!;
    private readonly PermisoRol _permiso;
    private readonly RevokePermissionFromRolCommandHandler _handler;

    public RevokePermissionFromRolCommandHandlerTests()
    {
        _permiso = PermisoRol.Create(_rolCajero.Id, "Ventas", "Crear").Value!;
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.EmpresaId).Returns(EmpresaId.ToString());
        _ = _dbContextMock.Setup(c => c.Roles).Returns(new List<Rol> { _rolCajero }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.PermisosRol).Returns(new List<PermisoRol> { _permiso }.BuildMockDbSet().Object);

        _handler = new RevokePermissionFromRolCommandHandler(_dbContextMock.Object, _currentUserMock.Object, _permissionCacheMock.Object);
    }

    [Fact]
    public async Task Handle_ExistingPermission_SoftDeletesAndInvalidatesCachedPermissionsOfRol()
    {
        var result = await _handler.Handle(new RevokePermissionFromRolCommand(_permiso.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _permiso.IsDeleted.Should().BeTrue();
        _permissionCacheMock.Verify(c => c.Invalidate(EmpresaId, "Cajero"), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownPermission_ReturnsNotFoundWithoutInvalidating()
    {
        var result = await _handler.Handle(new RevokePermissionFromRolCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Permiso.NotFound");
        _permissionCacheMock.Verify(c => c.Invalidate(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }
}
