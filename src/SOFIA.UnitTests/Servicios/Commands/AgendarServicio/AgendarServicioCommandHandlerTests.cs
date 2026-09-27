using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Servicios.Commands.AgendarServicio;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Servicios.Commands.AgendarServicio;

public class AgendarServicioCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly PacienteCliente _paciente = PacienteCliente.Create("12345678", "Juan Perez", new DateOnly(1990, 5, 10), null).Value!;
    private readonly Medicamento _producto = Medicamento.Create("SRV-001", "Vacuna Influenza", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
    private readonly List<ServicioAgenda> _agenda = [];
    private readonly AgendarServicioCommandHandler _handler;

    public AgendarServicioCommandHandlerTests()
    {
        _ = _dbContextMock.Setup(c => c.Pacientes).Returns(new List<PacienteCliente> { _paciente }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { _producto }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(new List<Venta>().BuildMockDbSet().Object);
        var agendaDbSet = _agenda.BuildMockDbSet();
        _ = agendaDbSet.Setup(d => d.Add(It.IsAny<ServicioAgenda>())).Callback<ServicioAgenda>(_agenda.Add);
        _ = _dbContextMock.Setup(c => c.ServiciosAgenda).Returns(agendaDbSet.Object);

        _handler = new AgendarServicioCommandHandler(_dbContextMock.Object);
    }

    private AgendarServicioCommand Command() => new(_paciente.Id, _producto.Id, null, DateTime.UtcNow.AddDays(2));

    [Fact]
    public async Task Handle_ShouldScheduleService_WhenReferencesBelongToCompany()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _agenda.Should().ContainSingle();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPatientIsNotInCompany()
    {
        var result = await _handler.Handle(Command() with { ClienteId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.StatusCode.Should().Be(404);
        _ = result.Error.Code.Should().Be("Paciente.NotFound");
        _ = _agenda.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductIsNotInCompany()
    {
        var result = await _handler.Handle(Command() with { ProductoId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.StatusCode.Should().Be(404);
        _ = result.Error.Code.Should().Be("Medicamento.NotFound");
        _ = _agenda.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenSaleIsNotInCompany()
    {
        var result = await _handler.Handle(Command() with { VentaId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.StatusCode.Should().Be(404);
        _ = result.Error.Code.Should().Be("Venta.NotFound");
        _ = _agenda.Should().BeEmpty();
    }
}
