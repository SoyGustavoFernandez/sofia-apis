using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.POS.Commands.AperturarCaja;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.POS.Commands.AperturarCaja;

public class AperturarCajaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock;
    private readonly AperturarCajaCommandHandler _handler;
    private readonly List<PosSesionCaja> _sesionesList = [];

    private readonly Guid _sucursalId = Guid.NewGuid();
    private readonly Guid _empleadoId = Guid.NewGuid();
    private readonly DateTime _fechaApertura = new(2026, 1, 15, 8, 0, 0, DateTimeKind.Utc);

    public AperturarCajaCommandHandlerTests()
    {
        _dbContextMock = new Mock<IApplicationDbContext>();

        var sesionesDbSetMock = _sesionesList.BuildMockDbSet();
        _ = sesionesDbSetMock.Setup(d => d.Add(It.IsAny<PosSesionCaja>())).Callback<PosSesionCaja>(_sesionesList.Add);
        _ = _dbContextMock.Setup(c => c.POSSesionesCaja).Returns(sesionesDbSetMock.Object);

        _handler = new AperturarCajaCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldCreateSesion_WhenAllFieldsAreValid()
    {
        var command = new AperturarCajaCommand(_sucursalId, _empleadoId, _fechaApertura, 500m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesionesList.Should().ContainSingle();
        _ = _sesionesList[0].SucursalId.Should().Be(_sucursalId);
        _ = _sesionesList[0].EmpleadoId.Should().Be(_empleadoId);
        _ = _sesionesList[0].MontoAperturaEfectivo.Should().Be(500m);
        _ = result.Value.Should().Be(_sesionesList[0].Id);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenSucursalIdIsEmpty()
    {
        var command = new AperturarCajaCommand(Guid.Empty, _empleadoId, _fechaApertura, 500m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.SucursalId");
        _ = _sesionesList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenEmpleadoIdIsEmpty()
    {
        var command = new AperturarCajaCommand(_sucursalId, Guid.Empty, _fechaApertura, 500m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.EmpleadoId");
        _ = _sesionesList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnError_WhenMontoAperturaIsNegative()
    {
        var command = new AperturarCajaCommand(_sucursalId, _empleadoId, _fechaApertura, -1m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("PosSesionCaja.MontoAperturaEfectivo");
        _ = _sesionesList.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSucceed_WhenMontoAperturaIsZero()
    {
        var command = new AperturarCajaCommand(_sucursalId, _empleadoId, _fechaApertura, 0m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _sesionesList.Should().ContainSingle();
    }
}
