using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Laboratorios.Commands.UpdateLaboratorio;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Laboratorios.Commands.UpdateLaboratorio;

public class UpdateLaboratorioCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Laboratorio _laboratorio = Laboratorio.Create("Bayer", "BAY-01").Value!;
    private readonly List<Laboratorio> _laboratorios;
    private readonly UpdateLaboratorioCommandHandler _handler;

    public UpdateLaboratorioCommandHandlerTests()
    {
        _laboratorios = [_laboratorio];
        var laboratoriosMock = _laboratorios.BuildMockDbSet();
        _ = laboratoriosMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<Laboratorio?>(_laboratorio));
        _ = _dbContextMock.Setup(c => c.Laboratorios).Returns(laboratoriosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateLaboratorioCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenKeepingItsOwnNombreAndCodigo()
    {
        var result = await _handler.Handle(new UpdateLaboratorioCommand(_laboratorio.Id, "Bayer", "BAY-01"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotChange_WhenNombreBelongsToAnotherLaboratorio()
    {
        _laboratorios.Add(Laboratorio.Create("Pfizer", null).Value!);

        var result = await _handler.Handle(new UpdateLaboratorioCommand(_laboratorio.Id, "Pfizer", "BAY-01"), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Laboratorio.NombreCompania.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = _laboratorio.NombreCompania.Should().Be("Bayer");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenCodigoBelongsToAnotherLaboratorio()
    {
        _laboratorios.Add(Laboratorio.Create("Pfizer", "PFZ-01").Value!);

        var result = await _handler.Handle(new UpdateLaboratorioCommand(_laboratorio.Id, "Bayer", "PFZ-01"), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Laboratorio.CodigoIdentificador.Duplicado");
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenTheCodigoOnlyBelongsToADeletedLaboratorio()
    {
        var eliminado = Laboratorio.Create("Pfizer", "PFZ-01").Value!;
        eliminado.IsDeleted = true;
        _laboratorios.Add(eliminado);

        var result = await _handler.Handle(new UpdateLaboratorioCommand(_laboratorio.Id, "Bayer", "PFZ-01"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
