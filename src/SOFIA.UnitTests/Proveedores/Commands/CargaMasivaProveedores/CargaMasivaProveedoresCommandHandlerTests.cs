using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Proveedores.Commands.CargaMasivaProveedores;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Proveedores.Commands.CargaMasivaProveedores;

public class CargaMasivaProveedoresCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<ProveedorDistribuidor> _proveedores = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<ProveedorDistribuidor>> _proveedoresMock;
    private readonly CargaMasivaProveedoresCommandHandler _handler;

    public CargaMasivaProveedoresCommandHandlerTests()
    {
        _proveedoresMock = _proveedores.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Proveedores).Returns(_proveedoresMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CargaMasivaProveedoresCommandHandler(_dbContextMock.Object);
    }

    private static ProveedorImportRow Row(string razonSocial, string taxId) => new(razonSocial, taxId, null, null, 100m);

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseTaxIdOrRazonSocialExistsInTheDatabaseOrEarlierInTheFile()
    {
        _proveedores.Add(ProveedorDistribuidor.Create("Existente SAC", "20100000001", null, null).Value!);
        var command = new CargaMasivaProveedoresCommand(
        [
            Row("Nueva SAC", "20100000001"),
            Row("existente sac", "20100000009"),
            Row("Otra SAC", "20100000002"),
            Row("OTRA SAC", "20100000003"),
            Row("Tercera SAC", "20100000002"),
            Row("Cuarta SAC", "20100000004"),
        ]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(2);
        _proveedoresMock.Verify(m => m.Add(It.IsAny<ProveedorDistribuidor>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSaveRow_WhenTheKeysOnlyBelongToADeletedProveedor()
    {
        var eliminado = ProveedorDistribuidor.Create("Existente SAC", "20100000001", null, null).Value!;
        eliminado.IsDeleted = true;
        _proveedores.Add(eliminado);

        var result = await _handler.Handle(new CargaMasivaProveedoresCommand([Row("Existente SAC", "20100000001")]), CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }
}
