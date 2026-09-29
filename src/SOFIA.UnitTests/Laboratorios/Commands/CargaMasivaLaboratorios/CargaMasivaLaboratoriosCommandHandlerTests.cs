using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Laboratorios.Commands.CargaMasivaLaboratorios;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Laboratorios.Commands.CargaMasivaLaboratorios;

public class CargaMasivaLaboratoriosCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<Laboratorio> _laboratorios = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Laboratorio>> _laboratoriosMock;
    private readonly CargaMasivaLaboratoriosCommandHandler _handler;

    public CargaMasivaLaboratoriosCommandHandlerTests()
    {
        _laboratoriosMock = _laboratorios.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Laboratorios).Returns(_laboratoriosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CargaMasivaLaboratoriosCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseNombreOrCodigoAlreadyExists()
    {
        _laboratorios.Add(Laboratorio.Create("Bayer", "BAY-01").Value!);
        var command = new CargaMasivaLaboratoriosCommand([new("bayer", null), new("Pfizer", "BAY-01"), new("Roche", null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(1);
        _laboratoriosMock.Verify(m => m.Add(It.Is<Laboratorio>(l => l.NombreCompania == "Roche")), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldKeepOnlyTheFirstRow_WhenTheFileRepeatsAKey()
    {
        var command = new CargaMasivaLaboratoriosCommand([new("Bayer", "B-1"), new("BAYER", "B-2"), new("Pfizer", "b-1"), new("Roche", null), new("Abbott", "")]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.Value.Should().Be(3, because: "labs without code never collide with each other");
        _laboratoriosMock.Verify(m => m.Add(It.IsAny<Laboratorio>()), Times.Exactly(3));
    }

    [Fact]
    public async Task Handle_ShouldSaveRow_WhenTheKeyOnlyBelongsToADeletedLaboratorio()
    {
        var eliminado = Laboratorio.Create("Bayer", "BAY-01").Value!;
        eliminado.IsDeleted = true;
        _laboratorios.Add(eliminado);

        var result = await _handler.Handle(new CargaMasivaLaboratoriosCommand([new("Bayer", "BAY-01")]), CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }
}
