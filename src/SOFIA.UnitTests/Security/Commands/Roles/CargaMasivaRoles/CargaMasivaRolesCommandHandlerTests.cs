using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Security.Commands.Roles.CargaMasivaRoles;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Security.Commands.Roles.CargaMasivaRoles;

public class CargaMasivaRolesCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<Rol> _roles = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Rol>> _rolesMock;
    private readonly CargaMasivaRolesCommandHandler _handler;

    public CargaMasivaRolesCommandHandlerTests()
    {
        _rolesMock = _roles.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Roles).Returns(_rolesMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CargaMasivaRolesCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseNameExistsInTheDatabaseOrEarlierInTheFile()
    {
        _roles.Add(Rol.Create("Cajero", null).Value!);
        var command = new CargaMasivaRolesCommand([new("cajero", null, 1), new("Regente", null, 2), new("REGENTE", null, 3), new("Auditor", null, 1)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(2);
        _rolesMock.Verify(m => m.Add(It.IsAny<Rol>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSaveRow_WhenTheNameOnlyBelongsToADeletedRol()
    {
        var eliminado = Rol.Create("Cajero", null).Value!;
        eliminado.IsDeleted = true;
        _roles.Add(eliminado);

        var result = await _handler.Handle(new CargaMasivaRolesCommand([new("Cajero", null, 1)]), CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }
}
