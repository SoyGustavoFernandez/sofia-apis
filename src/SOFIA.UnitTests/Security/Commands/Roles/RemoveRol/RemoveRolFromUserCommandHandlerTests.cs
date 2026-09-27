using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Roles.RemoveRol;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Roles.RemoveRol;

public class RemoveRolFromUserCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Cuenta _cuenta = Cuenta.Create(Guid.NewGuid(), "dueno", "hash").Value!;
    private readonly Rol _rolCajero = Rol.Create("Cajero", null).Value!;
    private readonly Rol _rolAdmin = Rol.Create(Rol.AdminRoleName, null).Value!;
    private readonly RemoveRolFromUserCommandHandler _handler;

    public RemoveRolFromUserCommandHandlerTests()
    {
        _cuenta.AddRol(_rolCajero);
        _cuenta.AddRol(_rolAdmin);
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuenta }.BuildMockDbSet().Object);
        _ = _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());

        _handler = new RemoveRolFromUserCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_RemovesRegularRol_AndRotatesSecurityStamp()
    {
        var stampAnterior = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new RemoveRolFromUserCommand(_cuenta.Id, _rolCajero.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.Roles.Should().NotContain(_rolCajero);
        _ = _cuenta.SecurityStamp.Should().NotBe(stampAnterior, because: "a removed role must stop working immediately");
    }

    [Fact]
    public async Task Handle_SelfRemoval_ReturnsForbidden()
    {
        _ = _currentUserMock.Setup(u => u.Id).Returns(_cuenta.EmpleadoId.ToString());

        var result = await _handler.Handle(new RemoveRolFromUserCommand(_cuenta.Id, _rolCajero.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Rol.AutoAsignacion");
        _ = _cuenta.Roles.Should().Contain(_rolCajero);
    }

    [Fact]
    public async Task Handle_NonAdminRemovingAdminRol_ReturnsForbidden()
    {
        var result = await _handler.Handle(new RemoveRolFromUserCommand(_cuenta.Id, _rolAdmin.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Rol.AdminReservado");
        _ = result.StatusCode.Should().Be(403);
        _ = _cuenta.EsAdmin.Should().BeTrue(because: "a non-admin must not be able to lock the owner out");
    }

    [Fact]
    public async Task Handle_AdminRemovingAdminRol_Succeeds()
    {
        _ = _currentUserMock.Setup(u => u.IsInRole(Rol.AdminRoleName)).Returns(true);

        var result = await _handler.Handle(new RemoveRolFromUserCommand(_cuenta.Id, _rolAdmin.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.EsAdmin.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_RolNotAssigned_SucceedsWithoutChanges()
    {
        var stampAnterior = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new RemoveRolFromUserCommand(_cuenta.Id, Guid.NewGuid()), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.SecurityStamp.Should().Be(stampAnterior);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
