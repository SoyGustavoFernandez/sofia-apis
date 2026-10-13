using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Proveedores.Commands.DeleteProveedor;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Proveedores.Commands.DeleteProveedor;

public class DeleteProveedorCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly ProveedorDistribuidor _proveedor = ProveedorDistribuidor.Create("Nueva SAC", "20100000001", null, null).Value!;
    private readonly Mock<DbSet<ProveedorDistribuidor>> _proveedoresMock;
    private readonly DeleteProveedorCommandHandler _handler;

    public DeleteProveedorCommandHandlerTests()
    {
        _proveedoresMock = new List<ProveedorDistribuidor> { _proveedor }.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Proveedores).Returns(_proveedoresMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupPrecios();
        _handler = new DeleteProveedorCommandHandler(_dbContextMock.Object);
    }

    private void SetupPrecios(params HistorialPrecioProveedor[] precios) =>
        _dbContextMock.Setup(c => c.HistorialPreciosProveedor).Returns(precios.ToList().BuildMockDbSet().Object);

    private HistorialPrecioProveedor NewPrecio() => HistorialPrecioProveedor.Create(_proveedor.Id, Guid.NewGuid(), 5m, DateTime.UtcNow, null, 3).Value!;

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProveedorDoesNotExist()
    {
        var result = await _handler.Handle(new DeleteProveedorCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByPrecio()
    {
        SetupPrecios(NewPrecio());

        var result = await _handler.Handle(new DeleteProveedorCommand(_proveedor.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Proveedor.InUse");
        _ = result.StatusCode.Should().Be(409);
        _proveedoresMock.Verify(m => m.Remove(It.IsAny<ProveedorDistribuidor>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenOnlySoftDeletedPreciosReferenceIt()
    {
        var precio = NewPrecio();
        precio.IsDeleted = true;
        SetupPrecios(precio);

        var result = await _handler.Handle(new DeleteProveedorCommand(_proveedor.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(_proveedor.Id);
        _proveedoresMock.Verify(m => m.Remove(_proveedor), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenProveedorHasNoDependents()
    {
        var result = await _handler.Handle(new DeleteProveedorCommand(_proveedor.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(_proveedor.Id);
        _proveedoresMock.Verify(m => m.Remove(_proveedor), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
