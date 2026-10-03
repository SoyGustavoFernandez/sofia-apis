using SOFIA.Application.Dashboard.Queries.GetDashboardCaja;
using SOFIA.Application.Dashboard.Queries.GetDashboardDevoluciones;
using SOFIA.Application.Dashboard.Queries.GetDashboardVentas;
using SOFIA.Application.POS.Common;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Dashboard;

public class DashboardCajaDevolucionesIntegrationTests : BaseIntegrationTest
{
    private readonly DateTimeOffset _hoy = DashboardTestClock.HoyLima();
    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company, so the figures of one never leak into another
    public DashboardCajaDevolucionesIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        _seeder = new PosCatalogSeeder(DbContext);
    }

    private DateTime Utc(double horasDesdeHoy) => _hoy.AddHours(horasDesdeHoy).UtcDateTime;

    private async Task<(Guid SucursalId, Guid EmpleadoId, Guid LoteId)> SeedSucursalConLoteAsync()
    {
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto caja");
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 500m);
        return (sucursalId, empleadoId, lote.Id);
    }

    [Fact]
    public async Task Caja_ShouldReturnMySessionWithTheExpectedCashOfTheCashCount_WhenIHaveAnOpenSession()
    {
        var (sucursalId, empleadoId, loteId) = await SeedSucursalConLoteAsync();
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = empleadoId;
        var sesion = await _seeder.SeedSesionAbiertaAsync(sucursalId, empleadoId, 100m);
        var conVuelto = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 2m, [(MetodoPago.Efectivo, 50m)], sesion.Id);
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 3m, [(MetodoPago.Tarjeta, 30m)], sesion.Id);
        var anulada = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 1m, [(MetodoPago.Efectivo, 10m)], sesion.Id);
        _ = anulada.Anular("Error de digitacion");
        _ = await DbContext.SaveChangesAsync();
        _ = await _seeder.SeedDevolucionAsync(conVuelto, 1m, sesion.Id, 10m);

        var result = await Sender.Send(new GetDashboardCajaQuery());

        _ = result.IsSuccess.Should().BeTrue();
        var miSesion = result.Value!.MiSesion!;
        var esperadoDelArqueo = await ArqueoCajaCalculator.CalcularEfectivoEsperadoAsync(DbContext, sesion, CancellationToken.None);
        _ = miSesion.Id.Should().Be(sesion.Id);
        _ = miSesion.MontoAperturaEfectivo.Should().Be(100m);
        _ = miSesion.EfectivoEsperado.Should().Be(110m);
        _ = miSesion.EfectivoEsperado.Should().Be(esperadoDelArqueo);
        _ = miSesion.SucursalNombre.Should().Be((await DbContext.Sucursales.FindAsync(sucursalId))!.Nombre);
        _ = miSesion.FechaHoraApertura.Should().BeCloseTo(sesion.FechaHoraApertura, TimeSpan.FromSeconds(1));
        _ = result.Value.SesionesAbiertas.Select(s => s.Id).Should().Equal(sesion.Id);
    }

    [Fact]
    public async Task Caja_ShouldListTheOpenSessionsOfEveryBranch_WhenTheUserIsAdminWithoutEmployee()
    {
        var (SucursalA, ColegaA, SesionA, SesionB) = await SeedSesionesEnDosSucursalesAsync();
        CurrentUser.Admin = true;

        var result = await Sender.Send(new GetDashboardCajaQuery());

        var dto = result.Value!;
        _ = dto.MiSesion.Should().BeNull();
        _ = dto.SesionesAbiertas.Select(s => s.Id).Should().BeEquivalentTo([SesionA, SesionB]);
        _ = dto.SesionesAbiertas.Select(s => s.FechaHoraApertura).Should().BeInDescendingOrder();
        _ = dto.SesionesAbiertas.Should().OnlyContain(s => s.EmpleadoNombre == "Ana Perez" && s.SucursalNombre.StartsWith("Suc"));
        _ = dto.SesionesAbiertas.Should().OnlyContain(s => s.MontoAperturaEfectivo == 100m);
    }

    [Fact]
    public async Task Caja_ShouldListOnlyTheOpenSessionsOfTheOwnBranch_WhenTheUserIsNotAdminAndHasNoSession()
    {
        var (SucursalA, ColegaA, SesionA, SesionB) = await SeedSesionesEnDosSucursalesAsync();
        CurrentUser.Sucursal = SucursalA;
        CurrentUser.Empleado = ColegaA;

        var result = await Sender.Send(new GetDashboardCajaQuery());

        var dto = result.Value!;
        _ = dto.MiSesion.Should().BeNull(because: "the colleague's only session is already closed");
        _ = dto.SesionesAbiertas.Select(s => s.Id).Should().Equal(SesionA);
    }

    [Fact]
    public async Task Caja_ShouldReturnNoSessions_WhenTheUserHasNoBranchInScopeAndNoEmployee()
    {
        _ = await SeedSesionesEnDosSucursalesAsync();

        var result = await Sender.Send(new GetDashboardCajaQuery());

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.MiSesion.Should().BeNull();
        _ = result.Value.SesionesAbiertas.Should().BeEmpty();
    }

    [Fact]
    public async Task Devoluciones_ShouldCountTodaysReturnsOfTheOwnBranchAndKeepTheSalesGross_WhenReturnsArePartialAndFull()
    {
        var (SucursalA, EmpleadoA, Parcial, Total) = await SeedDevolucionesAsync();
        CurrentUser.Sucursal = SucursalA;
        CurrentUser.Empleado = EmpleadoA;

        var devoluciones = await Sender.Send(new GetDashboardDevolucionesQuery(_hoy));
        var ventas = await Sender.Send(new GetDashboardVentasQuery(_hoy));

        _ = devoluciones.Value.Should().Be(new DashboardDevolucionesDto(2, 30m));
        _ = Parcial.Estado.Should().Be(EstadoVenta.Completada);
        _ = Total.Estado.Should().Be(EstadoVenta.Devuelta);
        _ = ventas.Value!.Hoy.Should().Be(new VentasDiaDto(_hoy, 60m, 2), because: "returns never reduce the gross amount sold");
    }

    [Fact]
    public async Task Devoluciones_ShouldCountTheReturnsOfEveryBranch_WhenTheUserIsAdmin()
    {
        _ = await SeedDevolucionesAsync();
        CurrentUser.Admin = true;

        var result = await Sender.Send(new GetDashboardDevolucionesQuery(_hoy));

        _ = result.Value.Should().Be(new DashboardDevolucionesDto(3, 80m));
    }

    [Fact]
    public async Task Devoluciones_ShouldReturnZeros_WhenThereAreNoReturnsOrNoBranchInScope()
    {
        _ = await SeedDevolucionesAsync();

        var sinAlcance = await Sender.Send(new GetDashboardDevolucionesQuery(_hoy));

        _ = sinAlcance.IsSuccess.Should().BeTrue();
        _ = sinAlcance.Value.Should().Be(new DashboardDevolucionesDto(0, 0m));
    }

    [Fact]
    public async Task Devoluciones_ShouldFailValidation_WhenHoyIsNotAroundTheCurrentDay()
    {
        CurrentUser.Admin = true;

        var result = await Sender.Send(new GetDashboardDevolucionesQuery(_hoy.AddDays(3)));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("GetDashboardDevolucionesQuery.Hoy");
    }

    // Branch A: an open session of its base employee and a closed one of a colleague; branch B: an open session
    private async Task<(Guid SucursalA, Guid ColegaA, Guid SesionA, Guid SesionB)> SeedSesionesEnDosSucursalesAsync()
    {
        var (sucursalA, empleadoA) = await _seeder.SeedSucursalAsync();
        var (sucursalB, empleadoB) = await _seeder.SeedSucursalAsync();
        var colegaA = await _seeder.SeedEmpleadoAsync(sucursalA);
        var sesionA = await _seeder.SeedSesionAbiertaAsync(sucursalA, empleadoA);
        var sesionB = await _seeder.SeedSesionAbiertaAsync(sucursalB, empleadoB);
        var cerrada = await _seeder.SeedSesionAbiertaAsync(sucursalA, colegaA);
        _ = cerrada.Cerrar(DateTime.UtcNow.AddMinutes(1), 100m, 100m);
        _ = await DbContext.SaveChangesAsync();
        return (sucursalA, colegaA, sesionA.Id, sesionB.Id);
    }

    // Branch A today: a partial return (10) and a full one (20), plus one from yesterday; branch B today: a full return (50)
    private async Task<(Guid SucursalA, Guid EmpleadoA, Domain.Entities.Venta Parcial, Domain.Entities.Venta Total)> SeedDevolucionesAsync()
    {
        var (sucursalA, empleadoA, loteA) = await SeedSucursalConLoteAsync();
        var (sucursalB, empleadoB, loteB) = await SeedSucursalConLoteAsync();
        var sesionA = await _seeder.SeedSesionAbiertaAsync(sucursalA, empleadoA);
        var sesionB = await _seeder.SeedSesionAbiertaAsync(sucursalB, empleadoB);
        var parcial = await _seeder.SeedVentaCobradaAsync(sucursalA, empleadoA, loteA, 4m, [(MetodoPago.Efectivo, 40m)], sesionA.Id, Utc(1));
        var total = await _seeder.SeedVentaCobradaAsync(sucursalA, empleadoA, loteA, 2m, [(MetodoPago.Tarjeta, 20m)], sesionA.Id, Utc(2));
        var deAyer = await _seeder.SeedVentaCobradaAsync(sucursalA, empleadoA, loteA, 3m, [(MetodoPago.Efectivo, 30m)], sesionA.Id, Utc(-30));
        var deOtraSucursal = await _seeder.SeedVentaCobradaAsync(sucursalB, empleadoB, loteB, 5m, [(MetodoPago.Efectivo, 50m)], sesionB.Id, Utc(1));
        _ = await _seeder.SeedDevolucionAsync(parcial, 1m, sesionA.Id, 10m, fechaUtc: Utc(3));
        _ = await _seeder.SeedDevolucionAsync(total, 2m, sesionA.Id, 20m, MetodoPago.Tarjeta, Utc(4));
        _ = await _seeder.SeedDevolucionAsync(deAyer, 3m, sesionA.Id, 30m, fechaUtc: _hoy.AddSeconds(-1).UtcDateTime);
        _ = await _seeder.SeedDevolucionAsync(deOtraSucursal, 5m, sesionB.Id, 50m, fechaUtc: Utc(5));
        return (sucursalA, empleadoA, parcial, total);
    }
}
