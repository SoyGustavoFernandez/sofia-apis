using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.POS.Commands.AperturarCaja;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.POS.Commands.AperturarCaja;

public class AperturarCajaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<ICurrentUser> _currentUserMock = new();
    private readonly List<PosSesionCaja> _sesionesList = [];

    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _empleadoId = Guid.NewGuid();

    public AperturarCajaCommandHandlerTests()
    {
        _ = _currentUserMock.Setup(u => u.IsAuthenticated).Returns(true);
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns(_sucursalId.ToString());
        _ = _currentUserMock.Setup(u => u.Id).Returns(_empleadoId.ToString());
    }

    private AperturarCajaCommandHandler CreateHandler()
    {
        var sesionesDbSetMock = _sesionesList.BuildMockDbSet();
        _ = sesionesDbSetMock.Setup(d => d.Add(It.IsAny<PosSesionCaja>())).Callback<PosSesionCaja>(_sesionesList.Add);
        _ = _dbContextMock.Setup(c => c.POSSesionesCaja).Returns(sesionesDbSetMock.Object);

        return new AperturarCajaCommandHandler(_dbContextMock.Object, _currentUserMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldTakeCashierBranchAndTimeFromSession_WhenOpeningCaja()
    {
        var antes = DateTime.UtcNow;

        var result = await CreateHandler().Handle(new AperturarCajaCommand(500m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        var sesion = _sesionesList.Should().ContainSingle().Subject;
        _ = sesion.SucursalId.Should().Be(_sucursalId);
        _ = sesion.EmpleadoId.Should().Be(_empleadoId);
        _ = sesion.FechaHoraApertura.Should().BeOnOrAfter(antes).And.BeOnOrBefore(DateTime.UtcNow);
        _ = sesion.MontoAperturaEfectivo.Should().Be(500m);
        _ = result.Value.Should().Be(sesion.Id);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenCashierAlreadyHasOpenCaja()
    {
        _sesionesList.Add(PosSesionCaja.Create(_sucursalId, _empleadoId, DateTime.UtcNow.AddHours(-2), 100m).Value!);

        var result = await CreateHandler().Handle(new AperturarCajaCommand(500m), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(409);
        _ = result.Error.Code.Should().Be("PosSesionCaja.YaAbierta");
        _ = _sesionesList.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenAConcurrentRequestOpenedACajaFirst()
    {
        var handler = CreateHandler();
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("save failed", new InvalidOperationException("Cannot insert duplicate key row in object 'dbo.POS_Sesiones_Caja' with unique index 'UX_POS_Sesiones_Caja_Empleado_Abierta'.")));

        var result = await handler.Handle(new AperturarCajaCommand(500m), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(409);
        _ = result.Error.Code.Should().Be("PosSesionCaja.YaAbierta");
    }

    [Fact]
    public async Task Handle_ShouldRethrow_WhenSaveFailsForAnotherReason()
    {
        var handler = CreateHandler();
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateException("save failed", new InvalidOperationException("FK violation")));

        var act = () => handler.Handle(new AperturarCajaCommand(500m), CancellationToken.None);

        _ = await act.Should().ThrowAsync<DbUpdateException>();
    }

    [Fact]
    public async Task Handle_ShouldOpenCaja_WhenOnlyAnotherCashierHasOpenCaja()
    {
        _sesionesList.Add(PosSesionCaja.Create(_sucursalId, Guid.NewGuid(), DateTime.UtcNow.AddHours(-2), 100m).Value!);

        var result = await CreateHandler().Handle(new AperturarCajaCommand(500m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesionesList.Should().HaveCount(2);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenUserHasNoActiveBranch()
    {
        _ = _currentUserMock.Setup(u => u.SucursalId).Returns((string?)null);

        var result = await CreateHandler().Handle(new AperturarCajaCommand(500m), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Auth.Sucursal");
        _ = _sesionesList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenMontoAperturaIsNegative()
    {
        var result = await CreateHandler().Handle(new AperturarCajaCommand(-1m), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.MontoAperturaEfectivo");
        _ = _sesionesList.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenMontoAperturaIsZero()
    {
        var result = await CreateHandler().Handle(new AperturarCajaCommand(0m), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesionesList.Should().ContainSingle();
    }
}
