using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Empleados.Commands.UpdateEmpleado;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Empleados.Commands.UpdateEmpleado;

public class UpdateEmpleadoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Empleado>> _empleadosMock;
    private readonly Sucursal _sucursalActual = Sucursal.Create("Sede A", "Av. Uno 123", "LIC-001").Value!;
    private readonly Sucursal _sucursalNueva = Sucursal.Create("Sede B", "Av. Dos 456", "LIC-002").Value!;
    private readonly UpdateEmpleadoCommandHandler _handler;

    public UpdateEmpleadoCommandHandlerTests()
    {
        _empleadosMock = new List<Empleado>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(_empleadosMock.Object);
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(new List<Sucursal> { _sucursalActual, _sucursalNueva }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta>().BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _currentUserMock.Setup(c => c.Id).Returns(Guid.NewGuid().ToString());
        _handler = new UpdateEmpleadoCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    private void SetupFind(Empleado? entity) =>
        _empleadosMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(entity));

    private Empleado ExistingEmpleado()
    {
        var empleado = Empleado.Create(_sucursalActual.Id, "Ana", "Pérez", "Gómez").Value!;
        SetupFind(empleado);
        return empleado;
    }

    private static UpdateEmpleadoCommand Command(Guid id, Guid sucursalId, string nombres = "Nuevo") => new()
    {
        Id = id,
        Sucursal_Base_ID = sucursalId,
        Nombres = nombres,
        Apellido_Paterno = "Pereira",
        Apellido_Materno = "González",
    };

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenEmpleadoDoesNotExist()
    {
        SetupFind(null);

        var result = await _handler.Handle(Command(Guid.NewGuid(), _sucursalNueva.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndSave_WhenCommandValid()
    {
        var empleado = ExistingEmpleado();

        var result = await _handler.Handle(Command(empleado.Id, _sucursalActual.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = empleado.Nombres.Should().Be("Nuevo");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndNotSave_WhenDomainValidationFails()
    {
        var empleado = ExistingEmpleado();

        var result = await _handler.Handle(Command(empleado.Id, _sucursalActual.Id, nombres: ""), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = empleado.Nombres.Should().Be("Ana");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenNewSucursalIsNotInTenant()
    {
        var empleado = ExistingEmpleado();

        var result = await _handler.Handle(Command(empleado.Id, Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = empleado.Sucursal_Base_ID.Should().Be(_sucursalActual.Id);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbiddenAndNotSave_WhenEmpleadoChangesOwnSucursal()
    {
        var empleado = ExistingEmpleado();
        _ = _currentUserMock.Setup(c => c.Id).Returns(empleado.Id.ToString());

        var result = await _handler.Handle(Command(empleado.Id, _sucursalNueva.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.SucursalBase.AutoAsignacion");
        _ = result.StatusCode.Should().Be(403);
        _ = empleado.Sucursal_Base_ID.Should().Be(_sucursalActual.Id);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldAllowOwnUpdate_WhenSucursalIsUnchanged()
    {
        var empleado = ExistingEmpleado();
        _ = _currentUserMock.Setup(c => c.Id).Returns(empleado.Id.ToString());

        var result = await _handler.Handle(Command(empleado.Id, _sucursalActual.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = empleado.Nombres.Should().Be("Nuevo");
    }

    [Fact]
    public async Task Handle_ShouldInvalidateSecurityStamp_WhenSucursalChanges()
    {
        var empleado = ExistingEmpleado();
        var cuenta = Cuenta.Create(empleado.Id, "ana", "hash").Value!;
        var otraCuenta = Cuenta.Create(Guid.NewGuid(), "otro", "hash").Value!;
        var (stampAntes, otroStampAntes) = (cuenta.SecurityStamp, otraCuenta.SecurityStamp);
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { cuenta, otraCuenta }.BuildMockDbSet().Object);

        var result = await _handler.Handle(Command(empleado.Id, _sucursalNueva.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = empleado.Sucursal_Base_ID.Should().Be(_sucursalNueva.Id);
        _ = cuenta.SecurityStamp.Should().NotBe(stampAntes, because: "tokens issued for the old branch must die");
        _ = otraCuenta.SecurityStamp.Should().Be(otroStampAntes);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldKeepSecurityStamp_WhenSucursalIsUnchanged()
    {
        var empleado = ExistingEmpleado();
        var cuenta = Cuenta.Create(empleado.Id, "ana", "hash").Value!;
        var stampAntes = cuenta.SecurityStamp;
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(new List<Cuenta> { cuenta }.BuildMockDbSet().Object);

        var result = await _handler.Handle(Command(empleado.Id, _sucursalActual.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = cuenta.SecurityStamp.Should().Be(stampAntes);
    }
}
