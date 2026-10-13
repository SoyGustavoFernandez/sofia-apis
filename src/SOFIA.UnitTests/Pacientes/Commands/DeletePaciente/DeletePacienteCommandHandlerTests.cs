using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Pacientes.Commands.DeletePaciente;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Pacientes.Commands.DeletePaciente;

public class DeletePacienteCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly PacienteCliente _paciente = PacienteCliente.Create(TipoDocumentoIdentidad.Dni, "12345678", "Juan Perez", new DateOnly(1990, 1, 1), null).Value!;
    private Mock<DbSet<PacienteCliente>> _pacientesMock = null!;
    private readonly DeletePacienteCommandHandler _handler;

    public DeletePacienteCommandHandlerTests()
    {
        SetupPacientes(_paciente);
        _ = _dbContextMock.Setup(c => c.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        SetupDependents();
        _handler = new DeletePacienteCommandHandler(_dbContextMock.Object);
    }

    private void SetupPacientes(params PacienteCliente[] pacientes)
    {
        _pacientesMock = pacientes.ToList().BuildMockDbSet();
        _ = _dbContextMock.Setup(c => c.Pacientes).Returns(_pacientesMock.Object);
    }

    private void SetupDependents(
        RecetaMedica? receta = null,
        Venta? venta = null,
        ServicioAgenda? cita = null,
        ServicioClinicoInmunizacion? inmunizacion = null)
    {
        _ = _dbContextMock.Setup(c => c.Recetas).Returns(Of(receta).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(Of(venta).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.ServiciosAgenda).Returns(Of(cita).BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.ServiciosClinicosInmunizacion).Returns(Of(inmunizacion).BuildMockDbSet().Object);
    }

    private static List<T> Of<T>(T? item) where T : class => item is null ? [] : [item];

    private RecetaMedica NewReceta() => RecetaMedica.Create(_paciente.Id, Guid.NewGuid(), new DateOnly(2026, 1, 1)).Value!;

    private Venta NewVenta() => Venta.Create(Guid.NewGuid(), Guid.NewGuid(), _paciente.Id, null, [DetalleVenta.Create(Guid.NewGuid(), 1m, 10m, 8m).Value!]).Value!;

    private ServicioAgenda NewCita() => ServicioAgenda.Create(_paciente.Id, Guid.NewGuid(), null, DateTime.UtcNow.AddDays(1)).Value!;

    private ServicioClinicoInmunizacion NewInmunizacion() =>
        ServicioClinicoInmunizacion.Create(null, _paciente.Id, Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), "IM", "Brazo", 0.5m, DateTime.UtcNow, null, ModalidadRegistro.Corriente).Value!;

    private async Task AssertConflictAsync()
    {
        var result = await _handler.Handle(new DeletePacienteCommand(_paciente.Id), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Paciente.InUse");
        _ = result.StatusCode.Should().Be(409);
        _pacientesMock.Verify(m => m.Remove(It.IsAny<PacienteCliente>()), Times.Never);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPacienteDoesNotExist()
    {
        var result = await _handler.Handle(new DeletePacienteCommand(Guid.NewGuid()), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("NotFound");
        _ = result.StatusCode.Should().Be(404);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByReceta()
    {
        SetupDependents(receta: NewReceta());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByVenta()
    {
        SetupDependents(venta: NewVenta());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByCita()
    {
        SetupDependents(cita: NewCita());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldReturnConflict_WhenReferencedByInmunizacion()
    {
        SetupDependents(inmunizacion: NewInmunizacion());

        await AssertConflictAsync();
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenAllDependentsAreSoftDeleted()
    {
        var receta = NewReceta();
        var venta = NewVenta();
        var cita = NewCita();
        var inmunizacion = NewInmunizacion();
        receta.IsDeleted = venta.IsDeleted = cita.IsDeleted = inmunizacion.IsDeleted = true;
        SetupDependents(receta, venta, cita, inmunizacion);

        var result = await _handler.Handle(new DeletePacienteCommand(_paciente.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(_paciente.Id);
        _pacientesMock.Verify(m => m.Remove(_paciente), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRemoveAndSave_WhenPacienteHasNoDependents()
    {
        var result = await _handler.Handle(new DeletePacienteCommand(_paciente.Id), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value.Should().Be(_paciente.Id);
        _pacientesMock.Verify(m => m.Remove(_paciente), Times.Once);
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
