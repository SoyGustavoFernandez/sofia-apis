using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Profesionales.Commands.CargaMasivaProfesionalesSalud;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Profesionales.Commands.CargaMasivaProfesionalesSalud;

public class CargaMasivaProfesionalesSaludCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<ProfesionalSalud> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<ProfesionalSalud>> _setMock;
    private readonly CargaMasivaProfesionalesSaludCommandHandler _handler;

    public CargaMasivaProfesionalesSaludCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.ProfesionalesSalud).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CargaMasivaProfesionalesSaludCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseKeyExistsInTheDatabaseOrEarlierInTheFile()
    {
        _existentes.Add(ProfesionalSalud.Create("K-001", "Dra. Rios", null).Value!);
        var command = new CargaMasivaProfesionalesSaludCommand([new("k-001", "Dra. Rios", null), new("K-002", "Dra. Rios", null), new("k-002", "Dra. Rios", null), new("K-003", "Dra. Rios", null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(2);
        _setMock.Verify(m => m.Add(It.IsAny<ProfesionalSalud>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSaveRow_WhenTheKeyOnlyBelongsToADeletedRow()
    {
        var eliminado = ProfesionalSalud.Create("K-001", "Dra. Rios", null).Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CargaMasivaProfesionalesSaludCommand([new("K-001", "Dra. Rios", null)]), CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }
}
