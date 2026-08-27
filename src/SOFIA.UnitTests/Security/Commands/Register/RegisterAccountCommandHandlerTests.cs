using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Register;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Register;

public class RegisterAccountCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<IPasswordHasher> _passwordHasherMock;
    private readonly RegisterAccountCommandHandler _handler;

    private readonly Guid _empleadoId = Guid.NewGuid();
    private const string Username = "jperez";
    private const string Password = "SecurePass123!";
    private const string HashedPassword = "hashed_password";

    public RegisterAccountCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _passwordHasherMock = new Mock<IPasswordHasher>();
        _ = _passwordHasherMock.Setup(p => p.Hash(Password)).Returns(HashedPassword);
        _handler = new RegisterAccountCommandHandler(_dbContextMock.Object, _passwordHasherMock.Object);
    }

    private Empleado CreateEmpleado(Guid id)
    {
        var empleado = Empleado.Create(Guid.NewGuid(), "Juan", "Perez", "Lopez").Value!;
        empleado.SetId(id);
        return empleado;
    }

    private void SetupContext(List<Empleado> empleados, List<Cuenta> cuentas)
    {
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(empleados.BuildMockDbSet().Object);

        var cuentasDbSetMock = cuentas.BuildMockDbSet();
        _ = cuentasDbSetMock.Setup(d => d.Add(It.IsAny<Cuenta>()));
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(cuentasDbSetMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenEmpleadoDoesNotExist()
    {
        // Arrange
        SetupContext(empleados: [], cuentas: []);
        var command = new RegisterAccountCommand(_empleadoId, Username, Password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.NotFound");
        _ = result.Error.Type.Should().Be(Domain.Common.ErrorType.NotFound);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenEmpleadoAlreadyHasCuenta()
    {
        // Arrange
        var empleado = CreateEmpleado(_empleadoId);
        var cuentaExistente = Cuenta.Create(_empleadoId, "otro_usuario", HashedPassword).Value!;

        SetupContext(empleados: [empleado], cuentas: [cuentaExistente]);
        var command = new RegisterAccountCommand(_empleadoId, Username, Password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Auth.DuplicateAccount");
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenUsernameAlreadyTaken()
    {
        // Arrange
        var empleado = CreateEmpleado(_empleadoId);
        // Cuenta con diferente empleado pero el mismo username
        var cuentaConMismoUsername = Cuenta.Create(Guid.NewGuid(), Username, HashedPassword).Value!;

        SetupContext(empleados: [empleado], cuentas: [cuentaConMismoUsername]);
        var command = new RegisterAccountCommand(_empleadoId, Username, Password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Auth.DuplicateUsername");
    }

    [Fact]
    public async Task Handle_ShouldCreateCuenta_WhenAllChecksPass()
    {
        // Arrange
        var empleado = CreateEmpleado(_empleadoId);

        SetupContext(empleados: [empleado], cuentas: []);
        _ = _dbContextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var command = new RegisterAccountCommand(_empleadoId, Username, Password);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().NotBeEmpty();
        _passwordHasherMock.Verify(p => p.Hash(Password), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldNotCallSave_WhenAnyCheckFails()
    {
        // Arrange
        SetupContext(empleados: [], cuentas: []); // empleado no existe

        var command = new RegisterAccountCommand(_empleadoId, Username, Password);

        // Act
        _ = await _handler.Handle(command, CancellationToken.None);

        // Assert
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
