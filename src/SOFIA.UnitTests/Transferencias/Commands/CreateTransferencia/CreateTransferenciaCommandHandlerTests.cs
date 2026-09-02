using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Transferencias.Commands.CreateTransferencia;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Transferencias.Commands.CreateTransferencia;

public class CreateTransferenciaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly Mock<ICurrentUser> _currentUserMock;
    private readonly CreateTransferenciaCommandHandler _handler;

    private readonly Guid _sucursalOrigenId = Guid.NewGuid();
    private readonly Guid _sucursalDestinoId = Guid.NewGuid();
    private readonly Guid _emisorId = Guid.NewGuid();

    public CreateTransferenciaCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();
        _currentUserMock = new Mock<ICurrentUser>();

        _ = _currentUserMock.Setup(c => c.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(c => c.SucursalId).Returns(_sucursalOrigenId.ToString());
        _ = _currentUserMock.Setup(c => c.Id).Returns(_emisorId.ToString());

        _handler = new CreateTransferenciaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnUnauthorized_WhenUserIsNotAuthenticated()
    {
        _ = _currentUserMock.Setup(c => c.IsAuthenticated).Returns(false);

        var command = new CreateTransferenciaCommand(_sucursalDestinoId, [new(Guid.NewGuid(), 5)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Auth.Sucursal");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenOrigenEqualsDestino()
    {
        var command = new CreateTransferenciaCommand(_sucursalOrigenId, [new(Guid.NewGuid(), 5)]);

        SetupSucursales([_sucursalOrigenId]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.SucursalDestino");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenSucursalDestinoDoesNotExist()
    {
        SetupSucursales([]); // destino no existe

        var command = new CreateTransferenciaCommand(_sucursalDestinoId, [new(Guid.NewGuid(), 5)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.SucursalDestinoNotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenLoteNotInSucursalOrigen()
    {
        SetupSucursales([_sucursalDestinoId]);
        SetupInventario([]); // sin stock en origen

        var command = new CreateTransferenciaCommand(_sucursalDestinoId, [new(Guid.NewGuid(), 5)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.LoteNotFound");
    }

    [Fact]
    public async Task Handle_ShouldReturnValidationError_WhenStockIsInsufficient()
    {
        var loteId = Guid.NewGuid();
        SetupSucursales([_sucursalDestinoId]);
        SetupInventario([InventarioSucursal.Create(_sucursalOrigenId, loteId, 3m).Value!]); // solo 3

        var command = new CreateTransferenciaCommand(_sucursalDestinoId, [new(loteId, 10)]); // pide 10

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Transferencia.StockInsuficiente");
    }

    [Fact]
    public async Task Handle_ShouldCreateTransferencia_WhenRequestIsValid()
    {
        var loteId = Guid.NewGuid();
        SetupSucursales([_sucursalDestinoId]);
        SetupInventario([InventarioSucursal.Create(_sucursalOrigenId, loteId, 20m).Value!]);

        var transferenciasList = new List<Transferencia>();
        var transferenciasDbSet = transferenciasList.BuildMockDbSet();
        _ = transferenciasDbSet.Setup(d => d.Add(It.IsAny<Transferencia>())).Callback<Transferencia>(transferenciasList.Add);
        _ = _dbContextMock.Setup(c => c.Transferencias).Returns(transferenciasDbSet.Object);

        var command = new CreateTransferenciaCommand(_sucursalDestinoId, [new(loteId, 5)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().NotBeEmpty();
        _ = transferenciasList.Should().HaveCount(1);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    private void SetupSucursales(List<Guid> sucursalIds)
    {
        // Build minimal Sucursal-like items by reusing InventarioSucursal pattern:
        // We only need IDs to match AnyAsync, so we use a typed stub via the real entity.
        // Sucursal entities are seeded via Create() in each test that needs a real one;
        // here we just need the DbSet to return matching IDs for AnyAsync.
        var sucursales = sucursalIds
            .Select(id =>
            {
                var s = Sucursal.Create($"Sucursal {id}", "Dirección test", "LIC-001").Value!;
                s.SetId(id);
                return s;
            })
            .ToList();

        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(sucursales.BuildMockDbSet().Object);
    }

    private void SetupInventario(List<InventarioSucursal> inventario) => _ = _dbContextMock.Setup(c => c.LotesEnSucursal).Returns(inventario.BuildMockDbSet().Object);
}
