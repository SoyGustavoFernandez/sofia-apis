using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Inventarios.Commands.RegisterInventario;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Inventarios.Commands.RegisterInventario;

public class RegisterInventarioCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ISucursalAccess> _sucursalAccessMock = new();
    private readonly Sucursal _sucursal = Sucursal.Create("Central", "Av. Lima 123", "LIC-001").Value!;
    private readonly LoteInventario _lote = LoteInventario.Create(Guid.NewGuid(), "LOT-001", null, DateTimeOffset.UtcNow.AddYears(1)).Value!;

    private RegisterInventarioHandler CreateHandler()
    {
        _ = _dbContextMock.Setup(c => c.Sucursales).Returns(new List<Sucursal> { _sucursal }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.LotesInventario).Returns(new List<LoteInventario> { _lote }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.LotesEnSucursal).Returns(new List<InventarioSucursal>().BuildMockDbSet().Object);
        _ = _sucursalAccessMock.Setup(a => a.CanAccessAsync(_sucursal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(true);
        return new RegisterInventarioHandler(_dbContextMock.Object, _sucursalAccessMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateStockRow_WhenBranchHasNoneForTheBatch()
    {
        var handler = CreateHandler();
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var result = await handler.Handle(new RegisterInventarioCommand(_sucursal.Id, _lote.Id, 5), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenAConcurrentRequestCreatedTheStockRowFirst()
    {
        var handler = CreateHandler();
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("save failed", new InvalidOperationException("Cannot insert duplicate key row in object 'dbo.Inventario_Sucursal' with unique index 'UX_Inventario_Sucursal_Lote'.")));

        var result = await handler.Handle(new RegisterInventarioCommand(_sucursal.Id, _lote.Id, 5), CancellationToken.None);

        _ = result.Error.Code.Should().Be("InventarioSucursal.RegistroConcurrente");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_ShouldReturnForbidden_WhenBranchIsNotAllowed()
    {
        var handler = CreateHandler();
        _ = _sucursalAccessMock.Setup(a => a.CanAccessAsync(_sucursal.Id, It.IsAny<CancellationToken>())).ReturnsAsync(false);

        var result = await handler.Handle(new RegisterInventarioCommand(_sucursal.Id, _lote.Id, 5, true), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Inventario.Sucursal.NoPermitida");
        _ = result.StatusCode.Should().Be(403);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBranchIsNotInTheTenant()
    {
        var handler = CreateHandler();

        var result = await handler.Handle(new RegisterInventarioCommand(Guid.NewGuid(), _lote.Id, 5), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Sucursal.NotFound");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
