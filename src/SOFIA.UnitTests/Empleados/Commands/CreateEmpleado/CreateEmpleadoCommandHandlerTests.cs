using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Empleados.Commands.CreateEmpleado;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Empleados.Commands.CreateEmpleado;

public class CreateEmpleadoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Empleado>> _empleadosMock;
    private readonly Sucursal _sucursal = Sucursal.Create("Sede", "Av. Siempre Viva 123", "LIC-001").Value!;
    private readonly CreateEmpleadoCommandHandler _handler;

    public CreateEmpleadoCommandHandlerTests()
    {
        _empleadosMock = new List<Empleado>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(_empleadosMock.Object);
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(new List<Sucursal> { _sucursal }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _currentUserMock.Setup(c => c.EmpresaId).Returns(Guid.NewGuid().ToString());
        _handler = new CreateEmpleadoCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    private CreateEmpleadoCommand Command(string nombres = "Ana", Guid? sucursalId = null) => new()
    {
        Sucursal_Base_ID = sucursalId ?? _sucursal.Id,
        Nombres = nombres,
        Apellido_Paterno = "Pérez",
        Apellido_Materno = "Gómez",
    };

    [Fact]
    public async Task Handle_ShouldAddAndSave_WhenCommandValid()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(201);
        _empleadosMock.Verify(m => m.Add(It.IsAny<Empleado>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenSucursalIsNotInTenant()
    {
        var result = await _handler.Handle(Command(sucursalId: Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _empleadosMock.Verify(m => m.Add(It.IsAny<Empleado>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndNotSave_WhenDomainValidationFails()
    {
        var result = await _handler.Handle(Command(nombres: ""), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.Nombres");
        _empleadosMock.Verify(m => m.Add(It.IsAny<Empleado>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenLicenciaIsTaken()
    {
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(new List<Empleado> { Empleado.Create(_sucursal.Id, "Luis", "Rios", "Paz", "CQFP-1").Value! }.BuildMockDbSet().Object);

        var result = await _handler.Handle(Command() with { Licencia_Prof = "CQFP-1" }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Empleado.LicenciaProf.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenLicenciaOnlyBelongsToADeletedEmpleado()
    {
        var eliminado = Empleado.Create(_sucursal.Id, "Luis", "Rios", "Paz", "CQFP-1").Value!;
        eliminado.IsDeleted = true;
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(new List<Empleado> { eliminado }.BuildMockDbSet().Object);

        var result = await _handler.Handle(Command() with { Licencia_Prof = "CQFP-1" }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
