using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Pacientes.Commands.CreatePaciente;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Pacientes.Commands.CreatePaciente;

public class CreatePacienteCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly List<PacienteCliente> _existentes = [];
    private readonly Mock<Microsoft.EntityFrameworkCore.DbSet<PacienteCliente>> _setMock;
    private readonly CreatePacienteCommandHandler _handler;

    public CreatePacienteCommandHandlerTests()
    {
        _setMock = _existentes.BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Pacientes).Returns(_setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new CreatePacienteCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotSave_WhenDocIsTaken()
    {
        _existentes.Add(PacienteCliente.Create(TipoDocumentoIdentidad.Pasaporte, "K001", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!);

        var result = await _handler.Handle(new CreatePacienteCommand(TipoDocumentoIdentidad.Pasaporte, "K001", "Juan Perez", new DateOnly(1990, 1, 1), null), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Paciente.DocIdentidadGub.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _setMock.Verify(m => m.Add(It.IsAny<PacienteCliente>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldCreate_WhenDocOnlyBelongsToADeletedRow()
    {
        var eliminado = PacienteCliente.Create(TipoDocumentoIdentidad.Pasaporte, "K001", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new CreatePacienteCommand(TipoDocumentoIdentidad.Pasaporte, "K001", "Juan Perez", new DateOnly(1990, 1, 1), null), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _setMock.Verify(m => m.Add(It.IsAny<PacienteCliente>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldStoreTheDocumentType_OfTheNewPatient()
    {
        PacienteCliente? creado = null;
        _ = _setMock.Setup(m => m.Add(It.IsAny<PacienteCliente>())).Callback<PacienteCliente>(p => creado = p);

        var result = await _handler.Handle(new CreatePacienteCommand(TipoDocumentoIdentidad.Dni, "45781236", "Juan Perez", new DateOnly(1990, 1, 1), null), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = creado!.TipoDocumento.Should().Be(TipoDocumentoIdentidad.Dni);
        _ = creado.DocIdentidadGub.Should().Be("45781236");
    }

    [Fact]
    public async Task Handle_ShouldFailAndNotSave_WhenTheNumberDoesNotMatchItsType()
    {
        var result = await _handler.Handle(new CreatePacienteCommand(TipoDocumentoIdentidad.Dni, "K001", "Juan Perez", new DateOnly(1990, 1, 1), null), CancellationToken.None);

        _ = result.Error.Code.Should().Be("PacienteCliente.DocIdentidadGub.FormatoInvalido");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
