using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Roles.DeleteRol;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Roles.DeleteRol;

public class DeleteRolCommandHandlerTests
{
    private static readonly Guid EmpresaId = Guid.NewGuid();

    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<IPermissionCache> _permissionCacheMock = new();
    private readonly Rol _rolCajero = Rol.Create("Cajero", null).Value!;
    private readonly DeleteRolCommandHandler _handler;

    public DeleteRolCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.EmpresaId).Returns(EmpresaId.ToString());
        _ = _dbContextMock.Setup(c => c.Roles).Returns(new List<Rol> { _rolCajero }.BuildMockDbSet().Object);

        _handler = new DeleteRolCommandHandler(_dbContextMock.Object, _currentUserMock.Object, _permissionCacheMock.Object);
    }

    [Fact]
    public async Task Handle_UnusedRol_InvalidatesCachedPermissionsOfRol()
    {
        var result = await _handler.Handle(new DeleteRolCommand(_rolCajero.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _permissionCacheMock.Verify(c => c.Invalidate(EmpresaId, "Cajero"), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownRol_ReturnsNotFoundWithoutInvalidating()
    {
        var result = await _handler.Handle(new DeleteRolCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Rol.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _permissionCacheMock.Verify(c => c.Invalidate(It.IsAny<Guid>(), It.IsAny<string>()), Times.Never);
    }
}
