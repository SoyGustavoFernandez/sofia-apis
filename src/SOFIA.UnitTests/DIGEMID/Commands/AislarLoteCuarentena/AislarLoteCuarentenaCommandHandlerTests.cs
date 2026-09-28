using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.DIGEMID.Commands.AislarLoteCuarentena;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.DIGEMID.Commands.AislarLoteCuarentena;

public class AislarLoteCuarentenaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _empleadoId = Guid.NewGuid();
    private readonly LoteInventario _lote = LoteInventario.Create(Guid.NewGuid(), "LOT-001", null, DateTimeOffset.UtcNow.AddYears(1)).Value!;
    private readonly List<DigemidInventarioCuarentena> _cuarentenas = [];
    private readonly AislarLoteCuarentenaCommandHandler _handler;

    public AislarLoteCuarentenaCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns(_sucursalId.ToString());
        _ = _currentUserMock.Setup(u => u.Id).Returns(_empleadoId.ToString());

        _ = _dbContextMock.Setup(c => c.LotesInventario).Returns(new List<LoteInventario> { _lote }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.DetallesDevolucion).Returns(new List<DevolucionDetalle>().BuildMockDbSet().Object);
        var cuarentenasDbSet = _cuarentenas.BuildMockDbSet();
        _ = cuarentenasDbSet.Setup(d => d.Add(It.IsAny<DigemidInventarioCuarentena>())).Callback<DigemidInventarioCuarentena>(_cuarentenas.Add);
        _ = _dbContextMock.Setup(c => c.DigemidInventarioCuarentena).Returns(cuarentenasDbSet.Object);

        _handler = new AislarLoteCuarentenaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    private AislarLoteCuarentenaCommand Command(Guid? loteId = null, Guid? detalleDevId = null) =>
        new(loteId ?? _lote.Id, detalleDevId, 3m, "Producto vencido", "Retenido");

    [Fact]
    public async Task Handle_ShouldRecordSessionBranchEmployeeAndTime_WhenBatchExists()
    {
        var antes = DateTime.UtcNow;

        var result = await _handler.Handle(Command(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        var cuarentena = _cuarentenas.Should().ContainSingle().Subject;
        _ = cuarentena.SucursalId.Should().Be(_sucursalId);
        _ = cuarentena.EmpleadoRegistraId.Should().Be(_empleadoId);
        _ = cuarentena.FechaIngresoCuarentena.Should().BeOnOrAfter(antes);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBatchIsNotInCompany()
    {
        var result = await _handler.Handle(Command(loteId: Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(404);
        _ = result.Error.Code.Should().Be("LoteInventario.NotFound");
        _ = _cuarentenas.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenReturnLineIsNotInCompany()
    {
        var result = await _handler.Handle(Command(detalleDevId: Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("DetalleDevolucion.NotFound");
        _ = _cuarentenas.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserHasNoActiveBranch()
    {
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns((string?)null);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Auth.Sucursal");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
