using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Sucursales.Commands.CreateSucursal;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Sucursales.Commands.CreateSucursal;

public class CreateSucursalCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly List<Sucursal> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Sucursal>> _setMock;
    private readonly CreateSucursalCommandHandler _handler;

    public CreateSucursalCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(c => c.EmpresaId).Returns(Guid.NewGuid().ToString());
        _handler = new CreateSucursalCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenGerenteIsUnknownOrForeign()
    {
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(new List<Empleado>().BuildMockDbSet().Object);

        var result = await _handler.Handle(new CreateSucursalCommand { Nombre = "Sede", DireccionFisica = "Av. Uno 123", NumeroLicencia = "K-001", GerenteId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Empleado.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _setMock.Verify(m => m.Add(It.IsAny<Sucursal>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenGerenteExistsInTheTenant()
    {
        var gerente = Empleado.Create(Guid.NewGuid(), "Ana", "Perez", "Gomez").Value!;
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(new List<Empleado> { gerente }.BuildMockDbSet().Object);

        var result = await _handler.Handle(new CreateSucursalCommand { Nombre = "Sede", DireccionFisica = "Av. Uno 123", NumeroLicencia = "K-001", GerenteId = gerente.Id }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _setMock.Verify(m => m.Add(It.IsAny<Sucursal>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenLicenciaIsTaken()
    {
        _existentes.Add(Sucursal.Create("Sede", "Av. Uno 123", "K-001").Value!);

        var result = await _handler.Handle(new CreateSucursalCommand { Nombre = "Sede", DireccionFisica = "Av. Uno 123", NumeroLicencia = "K-001" }, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Sucursal.NumeroLicencia.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _setMock.Verify(m => m.Add(It.IsAny<Sucursal>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenLicenciaOnlyBelongsToADeletedRow()
    {
        var eliminado = Sucursal.Create("Sede", "Av. Uno 123", "K-001").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CreateSucursalCommand { Nombre = "Sede", DireccionFisica = "Av. Uno 123", NumeroLicencia = "K-001" }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _setMock.Verify(m => m.Add(It.IsAny<Sucursal>()), Times.Once);
    }
}
