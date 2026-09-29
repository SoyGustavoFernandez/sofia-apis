using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Proveedores.Commands.CreateProveedor;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Proveedores.Commands.CreateProveedor;

public class CreateProveedorCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<ProveedorDistribuidor> _proveedores = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<ProveedorDistribuidor>> _proveedoresMock;
    private readonly CreateProveedorCommandHandler _handler;

    public CreateProveedorCommandHandlerTests()
    {
        _proveedoresMock = _proveedores.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Proveedores).Returns(_proveedoresMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateProveedorCommandHandler(_dbContextMock.Object);
    }

    private static ProveedorDistribuidor Proveedor(string razonSocial, string taxId) => ProveedorDistribuidor.Create(razonSocial, taxId, null, null).Value!;

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenTaxIdIsTaken()
    {
        _proveedores.Add(Proveedor("Otra SAC", "20100000001"));

        var result = await _handler.Handle(new CreateProveedorCommand("Nueva SAC", "20100000001", null, null), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Proveedor.TaxId.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenRazonSocialIsTaken()
    {
        _proveedores.Add(Proveedor("Nueva SAC", "20100000001"));

        var result = await _handler.Handle(new CreateProveedorCommand("Nueva SAC", "20100000002", null, null), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Proveedor.RazonSocial.Duplicado");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenTheKeysOnlyBelongToADeletedProveedor()
    {
        var eliminado = Proveedor("Nueva SAC", "20100000001");
        eliminado.IsDeleted = true;
        _proveedores.Add(eliminado);

        var result = await _handler.Handle(new CreateProveedorCommand("Nueva SAC", "20100000001", null, null), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _proveedoresMock.Verify(m => m.Add(It.IsAny<ProveedorDistribuidor>()), Times.Once);
    }
}
