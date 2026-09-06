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
    private readonly CreateEmpleadoCommandHandler _handler;

    public CreateEmpleadoCommandHandlerTests()
    {
        _empleadosMock = new List<Empleado>().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(_empleadosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _currentUserMock.Setup(c => c.EmpresaId).Returns(Guid.NewGuid().ToString());
        _handler = new CreateEmpleadoCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    private static CreateEmpleadoCommand Command(string nombres = "Ana") => new()
    {
        Sucursal_Base_ID = Guid.NewGuid(),
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
    public async Task Handle_ShouldReturnFailureAndNotSave_WhenDomainValidationFails()
    {
        var result = await _handler.Handle(Command(nombres: ""), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.Nombres");
        _empleadosMock.Verify(m => m.Add(It.IsAny<Empleado>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
