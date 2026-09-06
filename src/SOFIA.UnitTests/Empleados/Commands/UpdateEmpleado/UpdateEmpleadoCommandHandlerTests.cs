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
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Empleado>> _empleadosMock;
    private readonly UpdateEmpleadoCommandHandler _handler;

    public UpdateEmpleadoCommandHandlerTests()
    {
        _empleadosMock = new List<Empleado>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(_empleadosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateEmpleadoCommandHandler(_dbContextMock.Object);
    }

    private void SetupFind(Empleado? entity) =>
        _empleadosMock
            .Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns(ValueTask.FromResult(entity));

    private static UpdateEmpleadoCommand Command(Guid id, string nombres = "Nuevo") => new()
    {
        Id = id,
        Sucursal_Base_ID = Guid.NewGuid(),
        Nombres = nombres,
        Apellido_Paterno = "Pereira",
        Apellido_Materno = "González",
    };

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenEmpleadoDoesNotExist()
    {
        SetupFind(null);

        var result = await _handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldUpdateAndSave_WhenCommandValid()
    {
        var empleado = Empleado.Create(Guid.NewGuid(), "Ana", "Pérez", "Gómez").Value!;
        SetupFind(empleado);

        var result = await _handler.Handle(Command(empleado.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = empleado.Nombres.Should().Be("Nuevo");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnFailureAndNotSave_WhenDomainValidationFails()
    {
        var empleado = Empleado.Create(Guid.NewGuid(), "Ana", "Pérez", "Gómez").Value!;
        SetupFind(empleado);

        var result = await _handler.Handle(Command(empleado.Id, nombres: ""), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = empleado.Nombres.Should().Be("Ana");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
