using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Roles.SetSucursales;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Roles.SetSucursales;

public class SetRolSucursalesCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<RolSucursal>> _rolesSucursalesMock = new List<RolSucursal>().BuildMockDbSet();
    private readonly Rol _rol = Rol.Create("Supervisor", null).Value!;
    private readonly Sucursal _sucursalA = Sucursal.Create("Sede A", "Av. A 123", "LIC-A").Value!;
    private readonly Sucursal _sucursalB = Sucursal.Create("Sede B", "Av. B 456", "LIC-B").Value!;
    private readonly SetRolSucursalesCommandHandler _handler;

    public SetRolSucursalesCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Roles).Returns(new List<Rol> { _rol }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(new List<Sucursal> { _sucursalA, _sucursalB }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.RolesSucursales).Returns(_rolesSucursalesMock.Object);
        _handler = new SetRolSucursalesCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReplaceAssignments_WhenAllBranchesBelongToTheTenant()
    {
        var result = await _handler.Handle(new SetRolSucursalesCommand(_rol.Id, [_sucursalA.Id, _sucursalB.Id, _sucursalA.Id]), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _rolesSucursalesMock.Verify(d => d.AddRangeAsync(It.Is<IEnumerable<RolSucursal>>(rs => rs.Count() == 2), It.IsAny<CancellationToken>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenABranchIsOutsideTheTenant()
    {
        var result = await _handler.Handle(new SetRolSucursalesCommand(_rol.Id, [_sucursalA.Id, Guid.NewGuid()]), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Sucursal.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _rolesSucursalesMock.Verify(d => d.AddRangeAsync(It.IsAny<IEnumerable<RolSucursal>>(), It.IsAny<CancellationToken>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldClearAssignments_WhenListIsEmpty()
    {
        var result = await _handler.Handle(new SetRolSucursalesCommand(_rol.Id, []), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
