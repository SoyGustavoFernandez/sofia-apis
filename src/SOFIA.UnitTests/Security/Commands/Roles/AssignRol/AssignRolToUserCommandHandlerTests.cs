using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Roles.AssignRol;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Roles.AssignRol;

public class AssignRolToUserCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Cuenta _cuenta = Cuenta.Create(Guid.NewGuid(), "cajero", "hash").Value!;
    private readonly Rol _rolCajero = Rol.Create("Cajero", null).Value!;
    private readonly Rol _rolAdmin = Rol.Create(Rol.AdminRoleName, null).Value!;
    private readonly AssignRolToUserCommandHandler _handler;

    public AssignRolToUserCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuenta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Roles).Returns(new List<Rol> { _rolCajero, _rolAdmin }.BuildMockDbSet().Object);
        _ = _currentUserMock.Setup(u => u.Id).Returns(Guid.NewGuid().ToString());

        _handler = new AssignRolToUserCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_AssignsRolToAnotherAccount_AndRotatesSecurityStamp()
    {
        var stampAnterior = _cuenta.SecurityStamp;

        var result = await _handler.Handle(new AssignRolToUserCommand(_cuenta.Id, _rolCajero.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.Roles.Should().Contain(_rolCajero);
        _ = _cuenta.SecurityStamp.Should().NotBe(stampAnterior, because: "the old token must not keep the previous role set");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_SelfAssignment_ReturnsForbidden()
    {
        _ = _currentUserMock.Setup(u => u.Id).Returns(_cuenta.EmpleadoId.ToString());

        var result = await _handler.Handle(new AssignRolToUserCommand(_cuenta.Id, _rolCajero.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Rol.AutoAsignacion");
        _ = result.StatusCode.Should().Be(403);
        _ = _cuenta.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_NonAdminAssigningAdminRol_ReturnsForbidden()
    {
        var result = await _handler.Handle(new AssignRolToUserCommand(_cuenta.Id, _rolAdmin.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Rol.AdminReservado");
        _ = result.StatusCode.Should().Be(403);
        _ = _cuenta.Roles.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_AdminAssigningAdminRol_Succeeds()
    {
        _ = _currentUserMock.Setup(u => u.IsInRole(Rol.AdminRoleName)).Returns(true);

        var result = await _handler.Handle(new AssignRolToUserCommand(_cuenta.Id, _rolAdmin.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuenta.EsAdmin.Should().BeTrue();
    }
}
