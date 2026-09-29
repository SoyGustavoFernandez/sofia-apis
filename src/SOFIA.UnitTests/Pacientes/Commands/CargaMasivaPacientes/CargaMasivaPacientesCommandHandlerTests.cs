using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Pacientes.Commands.CargaMasivaPacientes;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Pacientes.Commands.CargaMasivaPacientes;

public class CargaMasivaPacientesCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<PacienteCliente> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<PacienteCliente>> _setMock;
    private readonly CargaMasivaPacientesCommandHandler _handler;

    public CargaMasivaPacientesCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Pacientes).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CargaMasivaPacientesCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSkipRows_WhoseKeyExistsInTheDatabaseOrEarlierInTheFile()
    {
        _existentes.Add(PacienteCliente.Create("K-001", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!);
        var command = new CargaMasivaPacientesCommand([new("k-001", "Juan Perez", "01/01/1990", null), new("K-002", "Juan Perez", "01/01/1990", null), new("k-002", "Juan Perez", "01/01/1990", null), new("K-003", "Juan Perez", "01/01/1990", null)]);

        var result = await _handler.Handle(command, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(2);
        _setMock.Verify(m => m.Add(It.IsAny<PacienteCliente>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_ShouldSaveRow_WhenTheKeyOnlyBelongsToADeletedRow()
    {
        var eliminado = PacienteCliente.Create("K-001", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CargaMasivaPacientesCommand([new("K-001", "Juan Perez", "01/01/1990", null)]), CancellationToken.None);

        _ = result.Value.Should().Be(1);
    }
}
