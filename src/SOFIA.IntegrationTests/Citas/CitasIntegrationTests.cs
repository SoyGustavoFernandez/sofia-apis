using SOFIA.Application.CatalogoServicios.Commands.CreateServicioCatalogo;
using SOFIA.Application.CatalogoServicios.Commands.DeleteServicioCatalogo;
using SOFIA.Application.Citas.Commands.CerrarCita;
using SOFIA.Application.Citas.Commands.ProgramarCita;
using SOFIA.Application.Citas.Commands.ReprogramarCita;
using SOFIA.Application.Citas.Queries.GetCitas;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Citas;

public class CitasIntegrationTests : BaseIntegrationTest
{
    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company
    public CitasIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        _seeder = new PosCatalogSeeder(DbContext);
    }

    private async Task<(Guid PacienteId, Guid ServicioId)> SeedAsync()
    {
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = empleadoId;
        var paciente = PacienteCliente.Create(TipoDocumentoIdentidad.Dni, $"{Random.Shared.Next(10_000_000, 99_999_999)}", "Rosa Diaz", new DateOnly(1990, 1, 1), "987654321").Value!;
        _ = DbContext.Pacientes.Add(paciente);
        _ = await DbContext.SaveChangesAsync();
        var servicio = await Sender.Send(new CreateServicioCatalogoCommand("Medida de presión", 3m, false, true));
        return (paciente.Id, servicio.Value);
    }

    private static ProgramarCitaCommand Cita(Guid pacienteId, Guid servicioId, int horas = 2) =>
        new(pacienteId, servicioId, DateTimeOffset.UtcNow.AddHours(horas), true);

    [Fact]
    public async Task Programar_ShouldListTheAppointmentOfTheBranchWithItsPatientAndService()
    {
        var (pacienteId, servicioId) = await SeedAsync();

        _ = await Sender.Send(Cita(pacienteId, servicioId));
        var lista = await Sender.Send(new GetCitasQuery { PacienteNombre = "Rosa", Estado = EstadoCita.Programada });
        CurrentUser.Sucursal = Guid.NewGuid();
        var otraSucursal = await Sender.Send(new GetCitasQuery());

        var cita = lista.Value!.Items.Should().ContainSingle().Subject;
        _ = cita.ServicioNombre.Should().Be("Medida de presión");
        _ = cita.PacienteContacto.Should().Be("987654321");
        _ = otraSucursal.Value!.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task Programar_ShouldRejectThePast()
    {
        var (pacienteId, servicioId) = await SeedAsync();

        var result = await Sender.Send(Cita(pacienteId, servicioId, -2));

        _ = result.Error.Code.Should().Be("CitaServicio.FechaPasada");
    }

    [Theory]
    [InlineData(EstadoCita.Cancelada)]
    [InlineData(EstadoCita.NoAsistio)]
    public async Task Cerrar_ShouldFreezeTheAppointment(EstadoCita estado)
    {
        var (pacienteId, servicioId) = await SeedAsync();
        var id = (await Sender.Send(Cita(pacienteId, servicioId))).Value;

        var cerrada = await Sender.Send(new CerrarCitaCommand(id, estado));
        var cambio = await Sender.Send(new ReprogramarCitaCommand(id, servicioId, DateTimeOffset.UtcNow.AddDays(1), false));

        _ = cerrada.IsSuccess.Should().BeTrue();
        _ = cambio.Error.Code.Should().Be("CitaServicio.YaCerrada");
    }

    [Fact]
    public async Task Cerrar_ShouldNotAllowMarkingAsAttendedByHand()
    {
        var (pacienteId, servicioId) = await SeedAsync();
        var id = (await Sender.Send(Cita(pacienteId, servicioId))).Value;

        var result = await Sender.Send(new CerrarCitaCommand(id, EstadoCita.Atendida));

        _ = result.Error.Code.Should().Be("CitaServicio.Estado");
    }

    [Fact]
    public async Task DeleteService_ShouldBeBlockedWhileItHasAppointments()
    {
        var (pacienteId, servicioId) = await SeedAsync();
        _ = await Sender.Send(Cita(pacienteId, servicioId));

        var result = await Sender.Send(new DeleteServicioCatalogoCommand(servicioId));

        _ = result.Error.Code.Should().Be("ServicioCatalogo.EnUso");
    }
}
