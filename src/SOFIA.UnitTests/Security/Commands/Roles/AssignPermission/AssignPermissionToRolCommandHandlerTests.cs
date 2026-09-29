using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Roles.AssignPermission;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Roles.AssignPermission;

public class AssignPermissionToRolCommandHandlerTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();

    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IPermissionCache> _permissionCacheMock = new();
    private readonly List<PermisoRol> _permisos = [];
    private readonly Rol _rolSupervisor = Rol.Create("Supervisor", null).Value!;
    private readonly AssignPermissionToRolCommandHandler _handler;

    public AssignPermissionToRolCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.EmpresaId).Returns(EmpresaId.ToString());
        _ = _dbContextMock.Setup(c => c.Roles).Returns(new List<Rol> { _rolSupervisor }.BuildMockDbSet().Object);
        var permisosDbSet = _permisos.BuildMockDbSet();
        _ = permisosDbSet.Setup(d => d.Add(It.IsAny<PermisoRol>())).Callback<PermisoRol>(_permisos.Add);
        _ = _dbContextMock.Setup(c => c.PermisosRol).Returns(permisosDbSet.Object);

        _handler = new AssignPermissionToRolCommandHandler(_dbContextMock.Object, _currentUserMock.Object, _permissionCacheMock.Object);
    }

    [Fact]
    public async Task Handle_NonAdminGrantingPermissionToOwnRol_ReturnsForbidden()
    {
        _ = _currentUserMock.Setup(u => u.IsInRole("Supervisor")).Returns(true);

        var result = await _handler.Handle(new AssignPermissionToRolCommand(_rolSupervisor.Id, "Seguridad", "AsignarRoles"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Rol.PermisosPropios");
        _ = result.StatusCode.Should().Be(403);
        _ = _permisos.Should().BeEmpty();
        _permissionCacheMock.Verify(c => c.Invalidate(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_NonAdminGrantingPermissionToAnotherRol_Succeeds()
    {
        var result = await _handler.Handle(new AssignPermissionToRolCommand(_rolSupervisor.Id, "Ventas", "Leer"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _permisos.Should().ContainSingle(p => p.RolId == _rolSupervisor.Id && p.ModuloSistema == "Ventas");
    }

    [Fact]
    public async Task Handle_AdminGrantingPermissionToOwnRol_Succeeds()
    {
        _ = _currentUserMock.Setup(u => u.IsInRole("Supervisor")).Returns(true);
        _ = _currentUserMock.Setup(u => u.IsInRole(Rol.AdminRoleName)).Returns(true);

        var result = await _handler.Handle(new AssignPermissionToRolCommand(_rolSupervisor.Id, "Ventas", "Leer"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_NewPermission_InvalidatesCachedPermissionsOfRol()
    {
        var result = await _handler.Handle(new AssignPermissionToRolCommand(_rolSupervisor.Id, "Ventas", "Leer"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _permissionCacheMock.Verify(c => c.Invalidate(EmpresaId, "Supervisor"), Times.Once);
    }

    [Fact]
    public async Task Handle_RestoringRevokedPermission_InvalidatesCachedPermissionsOfRol()
    {
        var revoked = PermisoRol.Create(_rolSupervisor.Id, "Ventas", "Leer").Value!;
        revoked.IsDeleted = true;
        _permisos.Add(revoked);

        var result = await _handler.Handle(new AssignPermissionToRolCommand(_rolSupervisor.Id, "Ventas", "Leer"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = revoked.IsDeleted.Should().BeFalse();
        _permissionCacheMock.Verify(c => c.Invalidate(EmpresaId, "Supervisor"), Times.Once);
    }

    [Fact]
    public async Task Handle_DuplicatePermission_DoesNotInvalidateCache()
    {
        _permisos.Add(PermisoRol.Create(_rolSupervisor.Id, "Ventas", "Leer").Value!);

        var result = await _handler.Handle(new AssignPermissionToRolCommand(_rolSupervisor.Id, "Ventas", "Leer"), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Permiso.Duplicate");
        _permissionCacheMock.Verify(c => c.Invalidate(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }
}
