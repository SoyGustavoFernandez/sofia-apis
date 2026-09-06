using System.Runtime.CompilerServices;
using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Empleados.Commands.DeleteEmpleado;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Empleados.Commands.DeleteEmpleado;

public class DeleteEmpleadoCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private Mock<Microsoft.EntityFrameworkCore.DbSet<Empleado>> _empleadosMock = new List<Empleado>().BuildMockDbSet();
    private readonly DeleteEmpleadoCommandHandler _handler;

    public DeleteEmpleadoCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(() => _empleadosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new DeleteEmpleadoCommandHandler(_dbContextMock.Object);
    }

    private void SetupEmpleados(params Empleado[] empleados) =>
        _empleadosMock = empleados.ToList().BuildMockDbSet();

    private static Empleado EmpleadoWithManagedBranch()
    {
        var empleado = Empleado.Create(Guid.NewGuid(), "Ana", "Pérez", "Gómez").Value!;
        var sucursal = (Sucursal)RuntimeHelpers.GetUninitializedObject(typeof(Sucursal));
        typeof(Empleado)
            .GetField("<Sucursal_Gerenciada>k__BackingField", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .SetValue(empleado, sucursal);
        return empleado;
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenEmpleadoDoesNotExist()
    {
        SetupEmpleados();

        var result = await _handler.Handle(new DeleteEmpleadoCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenEmpleadoManagesABranch()
    {
        var empleado = EmpleadoWithManagedBranch();
        SetupEmpleados(empleado);

        var result = await _handler.Handle(new DeleteEmpleadoCommand(empleado.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Empleado.EsGerente");
        _ = result.StatusCode.Should().Be(409);
        _empleadosMock.Verify(m => m.Remove(It.IsAny<Empleado>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenEmpleadoDoesNotManageABranch()
    {
        var empleado = Empleado.Create(Guid.NewGuid(), "Ana", "Pérez", "Gómez").Value!;
        SetupEmpleados(empleado);

        var result = await _handler.Handle(new DeleteEmpleadoCommand(empleado.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(204);
        _empleadosMock.Verify(m => m.Remove(empleado), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
