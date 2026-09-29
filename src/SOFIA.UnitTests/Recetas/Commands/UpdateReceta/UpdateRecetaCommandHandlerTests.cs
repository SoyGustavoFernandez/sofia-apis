using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Recetas.Commands.UpdateReceta;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Recetas.Commands.UpdateReceta;

public class UpdateRecetaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly PacienteCliente _paciente = PacienteCliente.Create("12345678", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!;
    private readonly ProfesionalSalud _medico = ProfesionalSalud.Create("CMP-001", "Dra. Rosa Diaz", null).Value!;
    private readonly RecetaMedica _receta;
    private readonly UpdateRecetaCommandHandler _handler;

    public UpdateRecetaCommandHandlerTests()
    {
        _receta = RecetaMedica.Create(_paciente.Id, _medico.Id, Hoy).Value!;
        _ = _dbContextMock.Setup(c => c.Recetas).Returns(new List<RecetaMedica> { _receta }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Pacientes).Returns(new List<PacienteCliente> { _paciente }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.ProfesionalesSalud).Returns(new List<ProfesionalSalud> { _medico }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdateRecetaCommandHandler(_dbContextMock.Object);
    }

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Handle_ShouldSave_WhenReferencesAreUnchanged()
    {
        var result = await _handler.Handle(new UpdateRecetaCommand(_receta.Id, _paciente.Id, _medico.Id, Hoy, 2, "Cada 8 horas"), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _receta.RepeticionesMax.Should().Be(2);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotChange_WhenNewPacienteIsUnknownOrForeign()
    {
        var result = await _handler.Handle(new UpdateRecetaCommand(_receta.Id, Guid.NewGuid(), _medico.Id, Hoy, 0, null), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Paciente.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = _receta.ClienteId.Should().Be(_paciente.Id);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotChange_WhenNewMedicoIsUnknownOrForeign()
    {
        var result = await _handler.Handle(new UpdateRecetaCommand(_receta.Id, _paciente.Id, Guid.NewGuid(), Hoy, 0, null), CancellationToken.None);

        _ = result.Error.Code.Should().Be("ProfesionalSalud.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _ = _receta.MedicoId.Should().Be(_medico.Id);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
