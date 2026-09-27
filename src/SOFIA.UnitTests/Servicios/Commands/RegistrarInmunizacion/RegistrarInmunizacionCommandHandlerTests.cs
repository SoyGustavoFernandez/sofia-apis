using FluentAssertions;
using MockQueryable.Moq;
using Moq;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Servicios.Commands.RegistrarInmunizacion;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.UnitTests.Servicios.Commands.RegistrarInmunizacion;

public class RegistrarInmunizacionCommandHandlerTests
{
    private readonly Mock<IApplicationDbContext> _dbContextMock = new();
    private readonly PacienteCliente _paciente = PacienteCliente.Create("12345678", "Juan Perez", new DateOnly(1990, 5, 10), null).Value!;
    private readonly Empleado _profesional = Empleado.Create(Guid.NewGuid(), "Rosa", "Quispe", "Mamani").Value!;
    private readonly Medicamento _vacuna = Medicamento.Create("VAC-001", "Vacuna Influenza", Guid.NewGuid(), Guid.NewGuid(), CondicionVenta.VentaLibreOTC).Value!;
    private readonly LoteInventario _lote;
    private readonly List<ServicioClinicoInmunizacion> _inmunizaciones = [];
    private readonly RegistrarInmunizacionCommandHandler _handler;

    public RegistrarInmunizacionCommandHandlerTests()
    {
        _lote = LoteInventario.Create(_vacuna.Id, "LOT-VAC", null, DateTimeOffset.UtcNow.AddYears(1)).Value!;

        _ = _dbContextMock.Setup(c => c.Pacientes).Returns(new List<PacienteCliente> { _paciente }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Empleados).Returns(new List<Empleado> { _profesional }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Medicamentos).Returns(new List<Medicamento> { _vacuna }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.LotesInventario).Returns(new List<LoteInventario> { _lote }.BuildMockDbSet().Object);
        _ = _dbContextMock.Setup(c => c.Ventas).Returns(new List<Venta>().BuildMockDbSet().Object);
        var inmunizacionesDbSet = _inmunizaciones.BuildMockDbSet();
        _ = inmunizacionesDbSet.Setup(d => d.Add(It.IsAny<ServicioClinicoInmunizacion>())).Callback<ServicioClinicoInmunizacion>(_inmunizaciones.Add);
        _ = _dbContextMock.Setup(c => c.ServiciosClinicosInmunizacion).Returns(inmunizacionesDbSet.Object);

        _handler = new RegistrarInmunizacionCommandHandler(_dbContextMock.Object);
    }

    private RegistrarInmunizacionCommand Command() => new(
        null, _paciente.Id, _profesional.Id, _vacuna.Id, _lote.Id,
        "IM", "Deltoides", 0.5m, DateTime.UtcNow, null, ModalidadRegistro.Corriente);

    [Fact]
    public async Task Handle_ShouldRegister_WhenReferencesBelongToCompany()
    {
        var result = await _handler.Handle(Command(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _inmunizaciones.Should().ContainSingle();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenPatientIsNotInCompany()
    {
        var result = await _handler.Handle(Command() with { ClienteId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Paciente.NotFound");
        _ = _inmunizaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProfessionalIsNotInCompany()
    {
        var result = await _handler.Handle(Command() with { ProfesionalAdmnId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Empleado.NotFound");
        _ = _inmunizaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenProductIsNotInCompany()
    {
        var result = await _handler.Handle(Command() with { ProductoId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Medicamento.NotFound");
        _ = _inmunizaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenBatchBelongsToAnotherProduct()
    {
        var otroLote = LoteInventario.Create(Guid.NewGuid(), "LOT-OTRO", null, DateTimeOffset.UtcNow.AddYears(1)).Value!;
        _ = _dbContextMock.Setup(c => c.LotesInventario).Returns(new List<LoteInventario> { _lote, otroLote }.BuildMockDbSet().Object);

        var result = await _handler.Handle(Command() with { LoteId = otroLote.Id }, CancellationToken.None);

        _ = result.StatusCode.Should().Be(404);
        _ = result.Error.Code.Should().Be("LoteInventario.NotFound");
        _ = _inmunizaciones.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnNotFound_WhenSaleIsNotInCompany()
    {
        var result = await _handler.Handle(Command() with { VentaId = Guid.NewGuid() }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Venta.NotFound");
        _ = _inmunizaciones.Should().BeEmpty();
    }
}
