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
    private readonly PacienteCliente _paciente = PacienteCliente.Create(TipoDocumentoIdentidad.Dni, "12345678", "Juan Perez", new DateOnly(1990, 5, 10), null).Value!;
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

    // Expiry stored like the SPA sends it (-05:00) for an expiry day that started the given hours ago.
    private static DateTimeOffset CaducidadIniciadaHace(double horas) => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).AddHours(-horas);

    private void SetCaducidad(DateTimeOffset caducidad) =>
        typeof(LoteInventario).GetProperty(nameof(LoteInventario.FechaCaducidad))!.SetValue(_lote, caducidad);

    [Theory]
    [InlineData(24)]
    [InlineData(36)]
    public async Task Handle_ShouldReturnLoteVencido_WhenTheExpiryDayEndedBeforeTheDose(double horas)
    {
        SetCaducidad(CaducidadIniciadaHace(horas));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Inmunizacion.Lote.Vencido");
        _ = _inmunizaciones.Should().BeEmpty();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(23)]
    public async Task Handle_ShouldRegister_WhenTheLotExpiresOnTheDoseDay(double horas)
    {
        SetCaducidad(CaducidadIniciadaHace(horas));

        var result = await _handler.Handle(Command(), CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue();
        _ = _inmunizaciones.Should().ContainSingle();
        _dbContextMock.Verify(c => c.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_ShouldRegister_WhenTheDoseWasGivenBeforeTheLotExpired()
    {
        SetCaducidad(CaducidadIniciadaHace(36));

        var result = await _handler.Handle(Command() with { FechaAdmnFisica = DateTime.UtcNow.AddDays(-2) }, CancellationToken.None);

        _ = result.IsSuccess.Should().BeTrue(because: "the lot is judged at the administration date, not at the registration time");
        _ = _inmunizaciones.Should().ContainSingle();
    }

    [Fact]
    public async Task Handle_ShouldReturnLoteVencido_WhenTheDoseDateIsAfterTheExpiryDayOfAValidLot()
    {
        var result = await _handler.Handle(Command() with { FechaAdmnFisica = DateTime.UtcNow.AddYears(2) }, CancellationToken.None);

        _ = result.Error.Code.Should().Be("Inmunizacion.Lote.Vencido");
        _ = _inmunizaciones.Should().BeEmpty();
    }

    [Theory]
    [InlineData(10, 23, 59, false)]
    [InlineData(11, 0, 0, true)]
    public async Task Handle_ShouldReadTheDoseDateAsUtc_WhenItHasNoOffset(int dia, int hora, int minuto, bool vencido)
    {
        SetCaducidad(new DateTimeOffset(2026, 3, 10, 0, 0, 0, TimeSpan.Zero));
        var fechaSinOffset = new DateTime(2026, 3, dia, hora, minuto, 0, DateTimeKind.Unspecified);

        var result = await _handler.Handle(Command() with { FechaAdmnFisica = fechaSinOffset }, CancellationToken.None);

        _ = result.IsFailure.Should().Be(vencido);
        _ = _inmunizaciones.Should().HaveCount(vencido ? 0 : 1);
    }

    [Theory]
    [InlineData(23.9, false)]
    [InlineData(24.1, true)]
    public async Task Handle_ShouldKeepTheInstant_WhenTheDoseDateIsLocal(double horas, bool vencido)
    {
        SetCaducidad(CaducidadIniciadaHace(horas));

        var result = await _handler.Handle(Command() with { FechaAdmnFisica = DateTime.UtcNow.ToLocalTime() }, CancellationToken.None);

        _ = result.IsFailure.Should().Be(vencido);
        _ = _inmunizaciones.Should().HaveCount(vencido ? 0 : 1);
    }

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
