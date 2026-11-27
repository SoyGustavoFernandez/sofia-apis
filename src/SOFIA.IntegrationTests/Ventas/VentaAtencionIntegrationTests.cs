using Microsoft.EntityFrameworkCore;
using SOFIA.Application.CatalogoServicios.Commands.CreateServicioCatalogo;
using SOFIA.Application.Citas.Commands.ProgramarCita;
using SOFIA.Application.Citas.Queries.GetCitaById;
using SOFIA.Application.Ventas.Commands.AnularVenta;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Application.Ventas.Queries.GetVentaById;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Ventas;

// Charging a performed service against real SQL Server: price, stock, appointment state and voiding
public class VentaAtencionIntegrationTests : BaseIntegrationTest
{
    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company
    public VentaAtencionIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        _seeder = new PosCatalogSeeder(DbContext);
    }

    private sealed record Escenario(Guid SesionId, Guid PacienteId, Guid LoteId);

    private static string NuevoRuc()
    {
        int[] pesos = [5, 4, 3, 2, 7, 6, 5, 4, 3, 2];
        var cuerpo = $"20{Random.Shared.Next(10_000_000, 99_999_999)}";
        var resto = 11 - (cuerpo.Select((c, i) => (c - '0') * pesos[i]).Sum() % 11);
        return $"{cuerpo}{resto % 10}";
    }

    private async Task<Escenario> SeedAsync()
    {
        var empresa = Empresa.Create($"Botica {Guid.NewGuid():N}"[..30], NuevoRuc()).Value!;
        CurrentUser.Empresa = empresa.Id;
        _ = DbContext.Empresas.Add(empresa);
        var sucursal = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. Test 123", $"LIC{Guid.NewGuid():N}"[..10], null, empresa.Id).Value!;
        _ = DbContext.Sucursales.Add(sucursal);
        _ = await DbContext.SaveChangesAsync();

        var empleadoId = await _seeder.SeedEmpleadoAsync(sucursal.Id);
        CurrentUser.Sucursal = sucursal.Id;
        CurrentUser.Empleado = empleadoId;
        _ = DbContext.SUNATSeriesFiscales.Add(SunatSerieFiscal.Create(sucursal.Id, TipoComprobante.Boleta, "B001", 0, SunatSerieFiscal.EstadoActiva).Value!);
        var paciente = PacienteCliente.Create(TipoDocumentoIdentidad.Dni, $"{Random.Shared.Next(10_000_000, 99_999_999)}", "Rosa Diaz", new DateOnly(1990, 1, 1), "987654321").Value!;
        _ = DbContext.Pacientes.Add(paciente);
        _ = await DbContext.SaveChangesAsync();

        var medicamento = await _seeder.SeedMedicamentoAsync("Ampolla de prueba");
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursal.Id, 5m);
        var sesion = await _seeder.SeedSesionAbiertaAsync(sucursal.Id, empleadoId);
        return new Escenario(sesion.Id, paciente.Id, lote.Id);
    }

    private async Task<Guid> ServicioAsync(string nombre, bool aplicaMedicamento = false, bool registraResultado = false) =>
        (await Sender.Send(new CreateServicioCatalogoCommand(nombre, 5m, aplicaMedicamento, registraResultado))).Value;

    private static CreateVentaCommand Venta(Escenario e, Guid? clienteId, decimal pago, CreateVentaAtencionDto atencion, params CreateVentaDetailDto[] detalles) =>
        new(clienteId, e.SesionId, [.. detalles], [new CreateVentaPagoDto(MetodoPago.Efectivo, pago, null)], Atencion: atencion);

    private Task<decimal> StockAsync(Guid loteId) => DbContext.LotesEnSucursal.AsNoTracking().Where(i => i.LoteId == loteId).Select(i => i.CantidadFisica).SingleAsync();

    [Fact]
    public async Task WalkIn_ShouldChargeOnlyTheServiceWithoutPatient_WhenItRecordsNothing()
    {
        var e = await SeedAsync();
        var servicioId = await ServicioAsync("Curacion");

        var result = await Sender.Send(Venta(e, null, 5m, new CreateVentaAtencionDto(servicioId)));
        var venta = await Sender.Send(new GetVentaByIdQuery(result.Value!.VentaId));

        _ = venta.Value!.Total.Should().Be(5m);
        _ = venta.Value.Detalles.Should().BeEmpty();
        _ = venta.Value.Servicios.Should().ContainSingle().Which.ServicioNombre.Should().Be("Curacion");
        var cita = await DbContext.CitasServicio.AsNoTracking().SingleAsync(c => c.VentaId == result.Value.VentaId);
        _ = cita.Estado.Should().Be(EstadoCita.Atendida);
    }

    [Fact]
    public async Task Attending_ShouldDeductStockAndReopenTheAppointment_WhenTheSaleIsVoided()
    {
        var e = await SeedAsync();
        var servicioId = await ServicioAsync("Inyectable", aplicaMedicamento: true);
        var citaId = (await Sender.Send(new ProgramarCitaCommand(e.PacienteId, servicioId, DateTimeOffset.UtcNow.AddHours(1), false))).Value;
        var atencion = new CreateVentaAtencionDto(servicioId, citaId, OrigenMedicamento.CompradoAhora, e.LoteId, Dosis: "1 ampolla");

        var result = await Sender.Send(Venta(e, e.PacienteId, 15m, atencion, new CreateVentaDetailDto(e.LoteId, 1m)));
        var atendida = await Sender.Send(new GetCitaByIdQuery(citaId));
        var stock = await StockAsync(e.LoteId);
        _ = await Sender.Send(new AnularVentaCommand(result.Value!.VentaId, "Error de cobro"));
        var reabierta = await Sender.Send(new GetCitaByIdQuery(citaId));

        _ = atendida.Value!.Estado.Should().Be(EstadoCita.Atendida);
        _ = atendida.Value.Dosis.Should().Be("1 ampolla");
        _ = stock.Should().Be(4m);
        _ = reabierta.Value!.Estado.Should().Be(EstadoCita.Programada);
        _ = reabierta.Value.VentaId.Should().BeNull();
        _ = (await StockAsync(e.LoteId)).Should().Be(5m);
    }

    [Fact]
    public async Task Attending_ShouldNotTouchStock_WhenThePatientBringsTheMedicine()
    {
        var e = await SeedAsync();
        var servicioId = await ServicioAsync("Inyectable propio", aplicaMedicamento: true);
        var atencion = new CreateVentaAtencionDto(servicioId, Origen: OrigenMedicamento.TraidoPorPaciente, MedicamentoTraido: "Complejo B", Dosis: "1 ampolla");

        var result = await Sender.Send(Venta(e, e.PacienteId, 5m, atencion));

        _ = result.IsSuccess.Should().BeTrue();
        _ = (await StockAsync(e.LoteId)).Should().Be(5m);
    }

    [Theory]
    [InlineData(false, "Atencion.PacienteRequerido")]
    [InlineData(true, "Atencion.ResultadoRequerido")]
    public async Task Attending_ShouldAskForWhatTheServiceRecords(bool conPaciente, string codigo)
    {
        var e = await SeedAsync();
        var servicioId = await ServicioAsync("Presion", registraResultado: true);

        var result = await Sender.Send(Venta(e, conPaciente ? e.PacienteId : null, 5m, new CreateVentaAtencionDto(servicioId)));

        _ = result.Error.Code.Should().Be(codigo);
        _ = (await DbContext.Ventas.AsNoTracking().AnyAsync(v => v.SesionId == e.SesionId)).Should().BeFalse();
    }
}
