using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Laboratorios.Commands.CreateLaboratorio;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Laboratorios.Commands.CreateLaboratorio;

public class CreateLaboratorioCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<Laboratorio> _laboratorios = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<Laboratorio>> _laboratoriosMock;
    private readonly CreateLaboratorioCommandHandler _handler;

    public CreateLaboratorioCommandHandlerTests()
    {
        _laboratoriosMock = _laboratorios.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Laboratorios).Returns(_laboratoriosMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateLaboratorioCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldAddAndSave_WhenCommandValid()
    {
        var result = await _handler.Handle(new CreateLaboratorioCommand("Bayer", "BAY-01"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.StatusCode.Should().Be(201);
        _laboratoriosMock.Verify(m => m.Add(It.IsAny<Laboratorio>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenNombreIsTaken()
    {
        _laboratorios.Add(Laboratorio.Create("Bayer", null).Value!);

        var result = await _handler.Handle(new CreateLaboratorioCommand("Bayer", "BAY-01"), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Laboratorio.NombreCompania.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenCodigoIsTaken()
    {
        _laboratorios.Add(Laboratorio.Create("Pfizer", "BAY-01").Value!);

        var result = await _handler.Handle(new CreateLaboratorioCommand("Bayer", "BAY-01"), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Laboratorio.CodigoIdentificador.Duplicado");
        _ = result.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenAnotherLaboratorioAlsoHasNoCodigo()
    {
        _laboratorios.Add(Laboratorio.Create("Pfizer", null).Value!);

        var result = await _handler.Handle(new CreateLaboratorioCommand("Bayer", "  "), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _laboratoriosMock.Verify(m => m.Add(It.Is<Laboratorio>(l => l.CodigoIdentificador == null)), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenTheSameKeysOnlyBelongToADeletedLaboratorio()
    {
        var eliminado = Laboratorio.Create("Bayer", "BAY-01").Value!;
        eliminado.IsDeleted = true;
        _laboratorios.Add(eliminado);

        var result = await _handler.Handle(new CreateLaboratorioCommand("Bayer", "BAY-01"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }
}
