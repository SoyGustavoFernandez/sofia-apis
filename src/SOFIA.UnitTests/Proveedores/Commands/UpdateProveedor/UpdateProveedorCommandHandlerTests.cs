using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Proveedores.Commands.UpdateProveedor;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Proveedores.Commands.UpdateProveedor;

public class UpdateProveedorCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly ProveedorDistribuidor _proveedor = ProveedorDistribuidor.Create("Nueva SAC", "20100000001", null, null).Value!;
    private readonly List<ProveedorDistribuidor> _proveedores;
    private readonly UpdateProveedorCommandHandler _handler;

    public UpdateProveedorCommandHandlerTests()
    {
        _proveedores = [_proveedor];
        var proveedoresMock = _proveedores.BuildMockDbSet();
        _ = proveedoresMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<ProveedorDistribuidor?>(_proveedor));
        _ = _dbContextMock.Setup(c => c.Proveedores).Returns(proveedoresMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateProveedorCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenKeepingItsOwnKeys()
    {
        var result = await _handler.Handle(new UpdateProveedorCommand(_proveedor.Id, "Nueva SAC", "20100000001", null, null, 90m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotChange_WhenTaxIdBelongsToAnotherProveedor()
    {
        _proveedores.Add(ProveedorDistribuidor.Create("Otra SAC", "20100000002", null, null).Value!);

        var result = await _handler.Handle(new UpdateProveedorCommand(_proveedor.Id, "Nueva SAC", "20100000002", null, null, 90m), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Proveedor.TaxId.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = _proveedor.TaxId.Should().Be("20100000001");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenTheRazonSocialOnlyBelongsToADeletedProveedor()
    {
        var eliminado = ProveedorDistribuidor.Create("Otra SAC", "20100000002", null, null).Value!;
        eliminado.IsDeleted = true;
        _proveedores.Add(eliminado);

        var result = await _handler.Handle(new UpdateProveedorCommand(_proveedor.Id, "Otra SAC", "20100000001", null, null, 90m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
