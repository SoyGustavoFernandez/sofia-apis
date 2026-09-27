using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Roles.AssignPermission;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Roles.AssignPermission;

public class AssignPermissionToRolCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly List<PermisoRol> _permisos = [];
    private readonly Rol _rolSupervisor = Rol.Create("Supervisor", null).Value!;
    private readonly AssignPermissionToRolCommandHandler _handler;

    public AssignPermissionToRolCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Roles).Returns(new List<Rol> { _rolSupervisor }.BuildMockDbSet().Object);
        var permisosDbSet = _permisos.BuildMockDbSet();
        _ = permisosDbSet.Setup(d => d.Add(It.IsAny<PermisoRol>())).Callback<PermisoRol>(_permisos.Add);
        _ = _dbContextMock.Setup(c => c.PermisosRol).Returns(permisosDbSet.Object);

        _handler = new AssignPermissionToRolCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
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
}
