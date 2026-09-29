using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Sucursales.Commands.CargaMasivaSucursales;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Sucursales.Commands.CargaMasivaSucursales;

public class CargaMasivaSucursalesCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly List<Sucursal> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Sucursal>> _setMock;
    private readonly CargaMasivaSucursalesCommandHandler _handler;

    public CargaMasivaSucursalesCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _ = _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(c => c.EmpresaId).Returns(Guid.NewGuid().ToString());
        _handler = new CargaMasivaSucursalesCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseKeyExistsInTheDatabaseOrEarlierInTheFile()
    {
        _existentes.Add(Sucursal.Create("Sede", "Av. Uno 123", "K-001").Value!);
        var command = new CargaMasivaSucursalesCommand([new("Sede", "Av. Uno 123", "k-001"), new("Sede", "Av. Uno 123", "K-002"), new("Sede", "Av. Uno 123", "k-002"), new("Sede", "Av. Uno 123", "K-003")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(2);
        _setMock.Verify(m => m.Add(It.IsAny<Sucursal>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSaveRow_WhenTheKeyOnlyBelongsToADeletedRow()
    {
        var eliminado = Sucursal.Create("Sede", "Av. Uno 123", "K-001").Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CargaMasivaSucursalesCommand([new("Sede", "Av. Uno 123", "K-001")]), CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }
}
