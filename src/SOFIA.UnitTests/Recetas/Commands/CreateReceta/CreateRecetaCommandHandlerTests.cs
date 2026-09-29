using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Recetas.Commands.CreateReceta;
using SOFIA.Domain.Entities;

namespace SOFIA.UnitTests.Recetas.Commands.CreateReceta;

public class CreateRecetaCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<RecetaMedica>> _recetasMock = new List<RecetaMedica>().BuildMockDbSet();
    private readonly PacienteCliente _paciente = PacienteCliente.Create("12345678", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!;
    private readonly ProfesionalSalud _medico = ProfesionalSalud.Create("CMP-001", "Dra. Rosa Diaz", null).Value!;
    private readonly CreateRecetaCommandHandler _handler;

    public CreateRecetaCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Recetas).Returns(_recetasMock.Object);
        _ = _dbContextMock.Setup(c => c.Pacientes).Returns(new List<PacienteCliente> { _paciente }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.ProfesionalesSalud).Returns(new List<ProfesionalSalud> { _medico }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreateRecetaCommandHandler(_dbContextMock.Object);
    }

    private static DateOnly Hoy => DateOnly.FromDateTime(DateTime.UtcNow);

    [Fact]
    public async Task Handle_ShouldAddAndSave_WhenPacienteAndMedicoBelongToTheTenant()
    {
        var result = await _handler.Handle(new CreateRecetaCommand(_paciente.Id, _medico.Id, Hoy), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _recetasMock.Verify(m => m.Add(It.IsAny<RecetaMedica>()), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenPacienteIsUnknownOrForeign()
    {
        var result = await _handler.Handle(new CreateRecetaCommand(Guid.NewGuid(), _medico.Id, Hoy), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Paciente.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _recetasMock.Verify(m => m.Add(It.IsAny<RecetaMedica>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFoundAndNotSave_WhenMedicoIsUnknownOrForeign()
    {
        var result = await _handler.Handle(new CreateRecetaCommand(_paciente.Id, Guid.NewGuid(), Hoy), CancellationToken.None);

        _ = result.Error.Code.Should().Be("ProfesionalSalud.NotFound");
        _ = result.StatusCode.Should().Be(404);
        _recetasMock.Verify(m => m.Add(It.IsAny<RecetaMedica>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
