using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Pacientes.Commands.UpdatePaciente;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Pacientes.Commands.UpdatePaciente;

public class UpdatePacienteCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly PacienteCliente _entity = PacienteCliente.Create(TipoDocumentoIdentidad.Pasaporte, "K001", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!;
    private readonly List<PacienteCliente> _existentes;
    private readonly UpdatePacienteCommandHandler _handler;

    public UpdatePacienteCommandHandlerTests()
    {
        _existentes = [_entity];
        var setMock = _existentes.BuildMockDbSet();
        _ = setMock.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>())).Returns(ValueTask.FromResult<PacienteCliente?>(_entity));
        _ = _dbContextMock.Setup(c => c.Pacientes).Returns(setMock.Object);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        _handler = new UpdatePacienteCommandHandler(_dbContextMock.Object);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenKeepingItsOwnDoc()
    {
        var result = await _handler.Handle(new UpdatePacienteCommand(_entity.Id, TipoDocumentoIdentidad.Pasaporte, "K001", "Juan Perez", new DateOnly(1990, 1, 1), null), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflictAndNotChange_WhenDocBelongsToAnotherRow()
    {
        _existentes.Add(PacienteCliente.Create(TipoDocumentoIdentidad.Pasaporte, "K002", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!);

        var result = await _handler.Handle(new UpdatePacienteCommand(_entity.Id, TipoDocumentoIdentidad.Pasaporte, "K002", "Juan Perez", new DateOnly(1990, 1, 1), null), CancellationToken.None);

        _ = result.Error.Code.Should().Be("Paciente.DocIdentidadGub.Duplicado");
        _ = result.StatusCode.Should().Be(409);
        _ = _entity.DocIdentidadGub.Should().Be("K001");
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldSave_WhenDocOnlyBelongsToADeletedRow()
    {
        var eliminado = PacienteCliente.Create(TipoDocumentoIdentidad.Pasaporte, "K002", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!;
        eliminado.IsDeleted = true;
        _existentes.Add(eliminado);

        var result = await _handler.Handle(new UpdatePacienteCommand(_entity.Id, TipoDocumentoIdentidad.Pasaporte, "K002", "Juan Perez", new DateOnly(1990, 1, 1), null), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_ShouldChangeTheDocumentType_WhenTheNewNumberMatchesIt()
    {
        var result = await _handler.Handle(new UpdatePacienteCommand(_entity.Id, TipoDocumentoIdentidad.Dni, "45781236", "Juan Perez", new DateOnly(1990, 1, 1), null), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _entity.TipoDocumento.Should().Be(TipoDocumentoIdentidad.Dni);
        _ = _entity.DocIdentidadGub.Should().Be("45781236");
    }

    [Fact]
    public async Task Handle_ShouldFailAndNotSave_WhenTheDocumentTypeIsMissing()
    {
        var result = await _handler.Handle(new UpdatePacienteCommand(_entity.Id, null, "K001", "Juan Perez", new DateOnly(1990, 1, 1), null), CancellationToken.None);

        _ = result.Error.Code.Should().Be("PacienteCliente.TipoDocumento");
        _ = _entity.TipoDocumento.Should().Be(TipoDocumentoIdentidad.Pasaporte);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
