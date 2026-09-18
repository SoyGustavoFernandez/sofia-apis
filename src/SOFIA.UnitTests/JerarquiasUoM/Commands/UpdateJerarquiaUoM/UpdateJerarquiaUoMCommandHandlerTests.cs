using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.JerarquiasUoM.Commands.UpdateJerarquiaUoM;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.JerarquiasUoM.Commands.UpdateJerarquiaUoM;

public class UpdateJerarquiaUoMCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<JerarquiaUoM> _jerarquiasList = [];
    private readonly UpdateJerarquiaUoMCommandHandler _handler;

    private readonly Guid _productoId = Guid.NewGuid();
    private readonly Guid _caja = Guid.NewGuid();
    private readonly Guid _blister = Guid.NewGuid();
    private readonly Guid _unidad = Guid.NewGuid();

    public UpdateJerarquiaUoMCommandHandlerTests()
    {
        var jerarquiasMock = _jerarquiasList.BuildMockDbSet();
        _ = jerarquiasMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
            .Returns<object[], CancellationToken>((ids, _) => ValueTask.FromResult(_jerarquiasList.SingleOrDefault(j => j.Id == (Guid)ids[0])));
        _ = _dbContextMock.Setup(c => c.JerarquiasUoM).Returns(jerarquiasMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        _handler = new UpdateJerarquiaUoMCommandHandler(_dbContextMock.Object);
    }

    private JerarquiaUoM Seed(Guid unidadMayorId, Guid unidadMenorId, decimal multiplicador)
    {
        var entity = JerarquiaUoM.Create(_productoId, unidadMayorId, unidadMenorId, multiplicador).Value!;
        _jerarquiasList.Add(entity);
        return entity;
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenJerarquiaDoesNotExist()
    {
        var command = new UpdateJerarquiaUoMCommand(Guid.NewGuid(), _productoId, _caja, _blister, 4m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("JerarquiaUoM.NotFound");
    }

    [Fact]
    public async Task Handle_ShouldUpdate_WhenNoOtherConversionConflicts()
    {
        var entity = Seed(_caja, _blister, 4m);

        var command = new UpdateJerarquiaUoMCommand(entity.Id, _productoId, _caja, _blister, 5m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = entity.Multiplicador.Should().Be(5m);
    }

    [Fact]
    public async Task Handle_ShouldNotConflictWithItself_WhenOnlyChangingItsOwnMultiplicador()
    {
        // Regression: excluding the row being edited from the consistency check is what
        // makes this possible — otherwise every update would "conflict" with its own prior value.
        var entity = Seed(_caja, _unidad, 40m);

        var command = new UpdateJerarquiaUoMCommand(entity.Id, _productoId, _caja, _unidad, 45m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = entity.Multiplicador.Should().Be(45m);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenNewValueContradictsAnotherExistingChain()
    {
        var entity = Seed(_caja, _unidad, 999m);
        _ = Seed(_caja, _blister, 4m);
        _ = Seed(_blister, _unidad, 10m);

        // The Caja -> Blister -> Unidad chain says 40; trying to set the direct row to 35 contradicts it.
        var command = new UpdateJerarquiaUoMCommand(entity.Id, _productoId, _caja, _unidad, 35m);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("JerarquiaUoM.Multiplicador.Inconsistente");
        _ = result.StatusCode.Should().Be(409);
        _ = entity.Multiplicador.Should().Be(999m);
    }
}
