using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.DIGEMID.Commands.GenerarActaDestruccion;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.DIGEMID.Commands.GenerarActaDestruccion;

public class GenerarActaDestruccionCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Empleado _regente = Empleado.Create(Guid.NewGuid(), "Rosa", "Quispe", "Mamani").Value!;
    private readonly List<DigemidActaDestruccion> _actas = [];
    private readonly GenerarActaDestruccionCommandHandler _handler;

    public GenerarActaDestruccionCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(new List<Empleado> { _regente }.BuildMockDbSet().Object);
        var actasDbSet = _actas.BuildMockDbSet();
        _ = actasDbSet.Setup(d => d.Add(It.IsAny<DigemidActaDestruccion>())).Callback<DigemidActaDestruccion>(_actas.Add);
        _ = _dbContextMock.Setup(c => c.DIGEMIDActasDestruccion).Returns(actasDbSet.Object);

        _handler = new GenerarActaDestruccionCommandHandler(_dbContextMock.Object);
    }

    private static GenerarActaDestruccionCommand Command(Guid regenteId) =>
        new("RES-2026-001", "EcoResiduos SAC", null, DateTime.UtcNow, regenteId, null);

    [Fact]
    public async Task Handle_ShouldCreateActa_WhenRegentBelongsToCompany()
    {
        var result = await _handler.Handle(Command(_regente.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _actas.Should().ContainSingle().Which.RegenteResponsableId.Should().Be(_regente.Id);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenRegentIsNotInCompany()
    {
        var result = await _handler.Handle(Command(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.StatusCode.Should().Be(404);
        _ = result.Error.Code.Should().Be("Empleado.NotFound");
        _ = _actas.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
