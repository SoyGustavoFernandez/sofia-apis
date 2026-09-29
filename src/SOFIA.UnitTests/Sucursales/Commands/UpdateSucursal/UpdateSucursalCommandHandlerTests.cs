using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Sucursales.Commands.UpdateSucursal;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Sucursales.Commands.UpdateSucursal;

public class UpdateSucursalCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Sucursal _entity = Sucursal.Create("Sede", "Av. Uno 123", "K-001").Value!;
    private readonly List<Sucursal> _existentes;
    private readonly UpdateSucursalCommandHandler _handler;

    public UpdateSucursalCommandHandlerTests()
    {
        _existentes = [_entity];
        var setMock = _existentes.BuildMockDbSet();
        _ = setMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<Sucursal?>(_entity));
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateSucursalCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenKeepingItsOwnLicencia()
    {
        var result = await _handler.Handle(new UpdateSucursalCommand { Id = _entity.Id, Nombre = "Sede", DireccionFisica = "Av. Uno 123", NumeroLicencia = "K-001" }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotChange_WhenNewGerenteIsUnknownOrForeign()
    {
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(new List<Empleado>().BuildMockDbSet().Object);

        var result = await _handler.Handle(new UpdateSucursalCommand { Id = _entity.Id, Nombre = "Sede", DireccionFisica = "Av. Uno 123", NumeroLicencia = "K-001", GerenteId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Empleado.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = _entity.Gerente_ID.Should().BeNull();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenNewGerenteExistsInTheTenant()
    {
        var gerente = Empleado.Create(Guid.NewGuid(), "Ana", "Perez", "Gomez").Value!;
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(new List<Empleado> { gerente }.BuildMockDbSet().Object);

        var result = await _handler.Handle(new UpdateSucursalCommand { Id = _entity.Id, Nombre = "Sede", DireccionFisica = "Av. Uno 123", NumeroLicencia = "K-001", GerenteId = gerente.Id }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _entity.Gerente_ID.Should().Be(gerente.Id);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotChange_WhenLicenciaBelongsToAnotherRow()
    {
        _existentes.Add(Sucursal.Create("Sede", "Av. Uno 123", "K-002").Value!);

        var result = await _handler.Handle(new UpdateSucursalCommand { Id = _entity.Id, Nombre = "Sede", DireccionFisica = "Av. Uno 123", NumeroLicencia = "K-002" }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Sucursal.NumeroLicencia.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = _entity.Numero_Licencia.Should().Be("K-001");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenLicenciaOnlyBelongsToADeletedRow()
    {
        var eliminado = Sucursal.Create("Sede", "Av. Uno 123", "K-002").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new UpdateSucursalCommand { Id = _entity.Id, Nombre = "Sede", DireccionFisica = "Av. Uno 123", NumeroLicencia = "K-002" }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
