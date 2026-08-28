using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Empresas.Commands.RegistrarEmpresa;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Empresas.Commands.RegistrarEmpresa;

public class RegistrarEmpresaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<IPasswordHasher> _passwordHasherMock = new();
    private readonly Mock<IJwtProvider> _jwtProviderMock = new();
    private readonly RegistrarEmpresaCommandHandler _handler;

    private const string FakeToken = "fake.jwt.token";
    private const string HashedPassword = "hashed_password";

    public RegistrarEmpresaCommandHandlerTests()
    {
        _ = _passwordHasherMock.Setup(p => p.Hash(It.IsAny<string>())).Returns(HashedPassword);
        _ = _jwtProviderMock
            .Setup(j => j.Generate(It.IsAny<Cuenta>(), It.IsAny<Guid?>(), It.IsAny<Guid?>()))
            .Returns(FakeToken);

        _handler = new RegistrarEmpresaCommandHandler(
            _dbContextMock.Object,
            _passwordHasherMock.Object,
            _jwtProviderMock.Object);
    }

    private void SetupEmptyContext()
    {
        var empresasDbSet = new List<Empresa>().BuildMockDbSet();
        _ = empresasDbSet.Setup(d => d.Add(It.IsAny<Empresa>()));
        _ = _dbContextMock.Setup(c => c.Empresas).Returns(empresasDbSet.Object);

        var cuentasDbSet = new List<Cuenta>().BuildMockDbSet();
        _ = cuentasDbSet.Setup(d => d.Add(It.IsAny<Cuenta>()));
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(cuentasDbSet.Object);

        var sucursalesDbSet = new List<Sucursal>().BuildMockDbSet();
        _ = sucursalesDbSet.Setup(d => d.Add(It.IsAny<Sucursal>()));
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(sucursalesDbSet.Object);

        var empleadosDbSet = new List<Empleado>().BuildMockDbSet();
        _ = empleadosDbSet.Setup(d => d.Add(It.IsAny<Empleado>()));
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(empleadosDbSet.Object);

        var rolesDbSet = new List<Rol>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Roles).Returns(rolesDbSet.Object);

        _ = _dbContextMock
            .Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
    }

    private static RegistrarEmpresaCommand MinimalCommand() => new()
    {
        NombreEmpresa = "Farmacia Salud",
        Usuario = "admin",
        Password = "Segura123"
    };

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenRUCAlreadyTaken()
    {
        var empresaExistente = Empresa.Create("Otra Farmacia", "12345678901").Value!;
        var empresasDbSet = new List<Empresa> { empresaExistente }.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Empresas).Returns(empresasDbSet.Object);

        var cuentasDbSet = new List<Cuenta>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(cuentasDbSet.Object);

        var command = MinimalCommand() with { RUC = "12345678901" };

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empresa.RUC.Duplicado");
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenUsernameAlreadyTaken()
    {
        var empresasDbSet = new List<Empresa>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Empresas).Returns(empresasDbSet.Object);

        var cuentaExistente = Cuenta.Create(Guid.NewGuid(), "admin", HashedPassword).Value!;
        var cuentasDbSet = new List<Cuenta> { cuentaExistente }.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Cuentas).Returns(cuentasDbSet.Object);

        var result = await _handler.Handle(MinimalCommand(), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Auth.DuplicateUsername");
    }

    [Fact]
    public async Task Handle_ShouldReturnJWT_WhenSuccessful()
    {
        SetupEmptyContext();

        var result = await _handler.Handle(MinimalCommand(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(FakeToken);
    }

    [Fact]
    public async Task Handle_ShouldSaveAllFourEntities_WhenSuccessful()
    {
        SetupEmptyContext();

        _ = await _handler.Handle(MinimalCommand(), CancellationToken.None);

        _dbContextMock.Verify(c => c.Empresas.Add(It.IsAny<Empresa>()), Times.Once);
        _dbContextMock.Verify(c => c.Sucursales.Add(It.IsAny<Sucursal>()), Times.Once);
        _dbContextMock.Verify(c => c.Empleados.Add(It.IsAny<Empleado>()), Times.Once);
        _dbContextMock.Verify(c => c.Cuentas.Add(It.IsAny<Cuenta>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldUseDefaultsForOptionalFields_WhenNotProvided()
    {
        SetupEmptyContext();
        Sucursal? capturedSucursal = null;
        _ = _dbContextMock
            .Setup(c => c.Sucursales.Add(It.IsAny<Sucursal>()))
            .Callback<Sucursal>(s => capturedSucursal = s);

        _ = await _handler.Handle(MinimalCommand(), CancellationToken.None);

        _ = capturedSucursal!.Nombre.Should().Be("Sede Principal");
        _ = capturedSucursal.Direccion_Fisica.Should().Be("Por definir");
        _ = capturedSucursal.Numero_Licencia.Should().Be("Por definir");
    }

    [Fact]
    public async Task Handle_ShouldUseProvidedOptionalFields_WhenSupplied()
    {
        SetupEmptyContext();
        Sucursal? capturedSucursal = null;
        _ = _dbContextMock
            .Setup(c => c.Sucursales.Add(It.IsAny<Sucursal>()))
            .Callback<Sucursal>(s => capturedSucursal = s);

        var command = MinimalCommand() with
        {
            NombreSede = "Local Norte",
            DireccionSede = "Av. Lima 123",
            NumeroLicencia = "LIC-001"
        };

        _ = await _handler.Handle(command, CancellationToken.None);

        _ = capturedSucursal!.Nombre.Should().Be("Local Norte");
        _ = capturedSucursal.Direccion_Fisica.Should().Be("Av. Lima 123");
        _ = capturedSucursal.Numero_Licencia.Should().Be("LIC-001");
    }

    [Fact]
    public async Task Handle_ShouldSkipRUCCheck_WhenRUCIsNull()
    {
        SetupEmptyContext();

        var result = await _handler.Handle(MinimalCommand() with { RUC = null }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        // Empresa check should NOT have been called with RUC filter
        _dbContextMock.Verify(c => c.Empresas, Times.Exactly(1)); // only for Add
    }

    [Fact]
    public async Task Handle_ShouldNotSave_WhenValidationFails()
    {
        SetupEmptyContext();

        // NombreEmpresa vacío → Empresa.Create falla
        var command = MinimalCommand() with { NombreEmpresa = "" };

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
