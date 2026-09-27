using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Cuentas.DeleteCuenta;
using SOFIA.Application.Security.Commands.Cuentas.UpdateCuenta;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Cuentas;

public class CuentaAdminProtectionTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Cuenta _cuentaAdmin = Cuenta.Create(Guid.NewGuid(), "dueno", "hash").Value!;
    private readonly Cuenta _cuentaCajero = Cuenta.Create(Guid.NewGuid(), "cajero", "hash").Value!;

    public CuentaAdminProtectionTests()
    {
        _cuentaAdmin.AddRol(Rol.Create(Rol.AdminRoleName, null).Value!);
        _cuentaCajero.AddRol(Rol.Create("Cajero", null).Value!);
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { _cuentaAdmin, _cuentaCajero }.BuildMockDbSet().Object);
    }

    [Fact]
    public async Task UpdateCuenta_NonAdminDeactivatingAdmin_ReturnsForbidden()
    {
        var handler = new UpdateCuentaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);

        var result = await handler.Handle(new UpdateCuentaCommand(_cuentaAdmin.Id, false, null, false), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Cuenta.AdminProtegida");
        _ = result.StatusCode.Should().Be(403);
        _ = _cuentaAdmin.CuentaActiva.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateCuenta_AdminDeactivatingAdmin_Succeeds()
    {
        _ = _currentUserMock.Setup(u => u.IsInRole(Rol.AdminRoleName)).Returns(true);
        var handler = new UpdateCuentaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);

        var result = await handler.Handle(new UpdateCuentaCommand(_cuentaAdmin.Id, false, null, false), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuentaAdmin.CuentaActiva.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateCuenta_NonAdminDeactivatingRegularAccount_Succeeds()
    {
        var handler = new UpdateCuentaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);

        var result = await handler.Handle(new UpdateCuentaCommand(_cuentaCajero.Id, false, null, false), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _cuentaCajero.CuentaActiva.Should().BeFalse();
    }

    [Fact]
    public async Task DeleteCuenta_NonAdminDeletingAdmin_ReturnsForbidden()
    {
        var handler = new DeleteCuentaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);

        var result = await handler.Handle(new DeleteCuentaCommand(_cuentaAdmin.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Cuenta.AdminProtegida");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteCuenta_AdminDeletingAdmin_Succeeds()
    {
        _ = _currentUserMock.Setup(u => u.IsInRole(Rol.AdminRoleName)).Returns(true);
        var handler = new DeleteCuentaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);

        var result = await handler.Handle(new DeleteCuentaCommand(_cuentaAdmin.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
