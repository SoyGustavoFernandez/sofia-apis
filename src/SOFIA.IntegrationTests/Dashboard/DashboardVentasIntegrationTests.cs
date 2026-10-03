using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Dashboard.Queries.GetDashboardVentas;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Dashboard;

public class DashboardVentasIntegrationTests : BaseIntegrationTest
{
    private readonly DateTimeOffset _hoy = DashboardTestClock.HoyLima();
    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company, so the figures of one never leak into another
    public DashboardVentasIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        _seeder = new PosCatalogSeeder(DbContext);
    }

    private DateTime Utc(double horasDesdeHoy) => _hoy.AddHours(horasDesdeHoy).UtcDateTime;

    private async Task<(Guid SucursalId, Guid EmpleadoId, Guid LoteId)> SeedSucursalConLoteAsync(string producto = "Producto dashboard")
    {
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync(producto);
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 500m);
        return (sucursalId, empleadoId, lote.Id);
    }

    [Fact]
    public async Task Handle_ShouldCountOnlyCollectedSalesNetOfChange_WhenThereAreVoidedAndParkedOnes()
    {
        var (sucursalId, empleadoId, loteId) = await SeedSucursalConLoteAsync();
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = empleadoId;
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 2m, [(MetodoPago.Efectivo, 50m)], fechaUtc: Utc(1));
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 3m, [(MetodoPago.Tarjeta, 30m)], fechaUtc: Utc(2));
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 4m, [(MetodoPago.YapePlin, 15m), (MetodoPago.Efectivo, 30m)], fechaUtc: Utc(3));
        var anulada = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 1m, [(MetodoPago.Efectivo, 10m)], fechaUtc: Utc(4));
        _ = anulada.Anular("Error de digitacion");
        _ = await DbContext.SaveChangesAsync();
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, loteId, 5m, EstadoVenta.Pendiente, Utc(5));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, loteId, 1m, EstadoVenta.Pendiente, Utc(-72));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, loteId, 6m, EstadoVenta.Devuelta, Utc(6));

        var result = await Sender.Send(new GetDashboardVentasQuery(_hoy));

        _ = result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        _ = dto.Hoy.Should().Be(new VentasDiaDto(_hoy, 150m, 4));
        _ = dto.TicketPromedio.Should().Be(37.5m);
        _ = dto.Mias.Should().Be(new VentasDiaDto(_hoy, 150m, 4));
        _ = dto.Pendientes.Should().Be(new VentasPendientesDto(2, 60m));
        _ = dto.PorMetodoPago.Should().Equal(
            new VentaMetodoPagoDto(MetodoPago.Efectivo, 45m),
            new VentaMetodoPagoDto(MetodoPago.YapePlin, 15m),
            new VentaMetodoPagoDto(MetodoPago.Tarjeta, 30m),
            new VentaMetodoPagoDto(MetodoPago.Transferencia, 0m));
        _ = dto.MultiSucursal.Should().BeFalse();
        _ = dto.EmpresaCompleta.Should().BeFalse();
        _ = dto.PorSucursal.Should().BeEmpty();
        _ = (await DbContext.VentasPagos.AsNoTracking().CountAsync(p => p.TransaccionId == anulada.Id)).Should().Be(1, because: "voiding keeps the payment rows, so the breakdown must filter by sale state");
    }

    [Fact]
    public async Task Handle_ShouldPlaceEachSaleOnItsLocalDay_WhenSalesFallAroundMidnightAndAfter19hLima()
    {
        var (sucursalId, empleadoId, loteId) = await SeedSucursalConLoteAsync();
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = empleadoId;
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 1m, Exacto(10m), fechaUtc: Utc(-4));
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 2m, Exacto(20m), fechaUtc: _hoy.AddSeconds(-1).UtcDateTime);
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 3m, Exacto(30m), fechaUtc: Utc(0));
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 4m, Exacto(40m), fechaUtc: Utc(20));
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 5m, Exacto(50m), fechaUtc: Utc(24));
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 6m, Exacto(60m), fechaUtc: Utc((-6 * 24) + 12));
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 7m, Exacto(70m), fechaUtc: Utc((-6 * 24) - 1));

        var result = await Sender.Send(new GetDashboardVentasQuery(_hoy));

        var dto = result.Value!;
        _ = Utc(-4).Date.Should().Be(Utc(0).Date, because: "a sale after 19:00 Lima already belongs to the next UTC day");
        _ = dto.Hoy.Should().Be(new VentasDiaDto(_hoy, 70m, 2));
        _ = dto.Ayer.Should().Be(new VentasDiaDto(_hoy.AddDays(-1), 30m, 2));
        _ = dto.Tendencia7Dias.Select(d => d.Desde).Should().Equal(Enumerable.Range(0, 7).Select(i => _hoy.AddDays(i - 6)));
        _ = dto.Tendencia7Dias.Select(d => d.Monto).Should().Equal(60m, 0m, 0m, 0m, 0m, 30m, 70m);
        _ = dto.Tendencia7Dias.Select(d => d.Comprobantes).Should().Equal(1, 0, 0, 0, 0, 2, 2);
        _ = dto.PorMetodoPago.Single(m => m.Metodo == MetodoPago.Efectivo).Monto.Should().Be(70m);
    }

    [Fact]
    public async Task Handle_ShouldBreakDownTheWholeCompanyByBranch_WhenTheUserIsAdmin()
    {
        var (SucursalA, EmpleadoA, SucursalB, SucursalSinVentas) = await SeedDosSucursalesAsync();
        CurrentUser.Admin = true;
        CurrentUser.Sucursal = SucursalA;
        CurrentUser.Empleado = EmpleadoA;

        var result = await Sender.Send(new GetDashboardVentasQuery(_hoy));

        var dto = result.Value!;
        _ = dto.MultiSucursal.Should().BeTrue();
        _ = dto.EmpresaCompleta.Should().BeTrue();
        _ = dto.Hoy.Should().Be(new VentasDiaDto(_hoy, 80m, 3));
        _ = dto.Mias.Should().Be(new VentasDiaDto(_hoy, 20m, 1));
        _ = dto.Pendientes.Should().Be(new VentasPendientesDto(2, 50m));
        _ = dto.PorSucursal.Should().HaveCount(3);
        _ = dto.PorSucursal.Select(s => s.Nombre).Should().BeInAscendingOrder();
        _ = dto.PorSucursal.Single(s => s.SucursalId == SucursalA).Should().BeEquivalentTo(new { Monto = 30m, Comprobantes = 2 });
        _ = dto.PorSucursal.Single(s => s.SucursalId == SucursalB).Should().BeEquivalentTo(new { Monto = 50m, Comprobantes = 1 });
        _ = dto.PorSucursal.Single(s => s.SucursalId == SucursalSinVentas).Should().BeEquivalentTo(new { Monto = 0m, Comprobantes = 0 });
        _ = dto.PorSucursal.Sum(s => s.Monto).Should().Be(dto.Hoy.Monto);
        _ = dto.PorSucursal.Sum(s => s.Comprobantes).Should().Be(dto.Hoy.Comprobantes);
    }

    [Fact]
    public async Task Handle_ShouldCoverOnlyTheOwnBranchAndTheOwnSales_WhenTheUserIsNotAdmin()
    {
        var (SucursalA, EmpleadoA, SucursalB, SucursalSinVentas) = await SeedDosSucursalesAsync();
        CurrentUser.Sucursal = SucursalA;
        CurrentUser.Empleado = EmpleadoA;

        var result = await Sender.Send(new GetDashboardVentasQuery(_hoy));

        var dto = result.Value!;
        _ = dto.MultiSucursal.Should().BeFalse();
        _ = dto.EmpresaCompleta.Should().BeFalse();
        _ = dto.PorSucursal.Should().BeEmpty();
        _ = dto.Hoy.Should().Be(new VentasDiaDto(_hoy, 30m, 2));
        _ = dto.Mias.Should().Be(new VentasDiaDto(_hoy, 20m, 1));
        _ = dto.Pendientes.Should().Be(new VentasPendientesDto(1, 10m));
        _ = dto.PorMetodoPago.Sum(m => m.Monto).Should().Be(30m);
    }

    [Fact]
    public async Task Handle_ShouldRankTheTopFiveProductsByBaseUnits_OverCountedSalesOfTheLastSevenDays()
    {
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = empleadoId;
        var lotes = new List<(Guid ProductoId, Guid LoteId)>();
        for (var i = 1; i <= 7; i++)
        {
            var medicamento = await _seeder.SeedMedicamentoAsync($"Top {i}");
            lotes.Add((medicamento.Id, (await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 500m)).Id));
        }

        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[0].LoteId, 1m, fechaUtc: Utc(1));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[0].LoteId, 50m, EstadoVenta.Anulada, Utc(1));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[0].LoteId, 50m, EstadoVenta.Pendiente, Utc(1));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[1].LoteId, 2m, fechaUtc: Utc(1));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[1].LoteId, 3m, fechaUtc: Utc(-5 * 24));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[2].LoteId, 3m, fechaUtc: Utc(1));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[3].LoteId, 4m, fechaUtc: Utc(-2 * 24));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[4].LoteId, 5m, fechaUtc: Utc(1));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[5].LoteId, 6m, EstadoVenta.Devuelta, Utc(1));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, lotes[6].LoteId, 90m, fechaUtc: Utc((-6 * 24) - 1));

        var result = await Sender.Send(new GetDashboardVentasQuery(_hoy));

        var top = result.Value!.TopProductos;
        _ = top.Select(p => p.Unidades).Should().Equal(6m, 5m, 5m, 4m, 3m);
        _ = top.Select(p => p.Monto).Should().Equal(60m, 50m, 50m, 40m, 30m);
        _ = top[0].Should().Be(new TopProductoDto(lotes[5].ProductoId, "Top 6", 6m, 60m));
        _ = top.Skip(1).Take(2).Select(p => p.Nombre).Should().BeEquivalentTo("Top 2", "Top 5");
        _ = top.Skip(3).Select(p => p.Nombre).Should().Equal("Top 4", "Top 3");
    }

    [Fact]
    public async Task Handle_ShouldReturnZerosAndNoPersonalFigures_WhenTheCompanyHasNoDataAndTheUserNoEmployee()
    {
        CurrentUser.Admin = true;

        var result = await Sender.Send(new GetDashboardVentasQuery(_hoy));

        _ = result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        _ = dto.MultiSucursal.Should().BeFalse();
        _ = dto.EmpresaCompleta.Should().BeTrue();
        _ = dto.Hoy.Should().Be(new VentasDiaDto(_hoy, 0m, 0));
        _ = dto.Ayer.Should().Be(new VentasDiaDto(_hoy.AddDays(-1), 0m, 0));
        _ = dto.TicketPromedio.Should().Be(0m);
        _ = dto.Mias.Should().BeNull();
        _ = dto.PorMetodoPago.Select(m => m.Metodo).Should().Equal(MetodoPago.Efectivo, MetodoPago.YapePlin, MetodoPago.Tarjeta, MetodoPago.Transferencia);
        _ = dto.PorMetodoPago.Should().OnlyContain(m => m.Monto == 0m);
        _ = dto.Pendientes.Should().Be(new VentasPendientesDto(0, 0m));
        _ = dto.PorSucursal.Should().BeEmpty();
        _ = dto.Tendencia7Dias.Should().HaveCount(7).And.OnlyContain(d => d.Monto == 0m && d.Comprobantes == 0);
        _ = dto.TopProductos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldReturnZeros_WhenTheUserHasNoBranchInScope()
    {
        var (sucursalId, empleadoId, loteId) = await SeedSucursalConLoteAsync();
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, loteId, 2m, [(MetodoPago.Efectivo, 20m)], fechaUtc: Utc(1));
        await _seeder.SeedVentaAsync(sucursalId, empleadoId, loteId, 1m, EstadoVenta.Pendiente, Utc(1));
        CurrentUser.Empleado = empleadoId;

        var result = await Sender.Send(new GetDashboardVentasQuery(_hoy));

        var dto = result.Value!;
        _ = dto.EmpresaCompleta.Should().BeFalse();
        _ = dto.Hoy.Should().Be(new VentasDiaDto(_hoy, 0m, 0));
        _ = dto.Mias.Should().Be(new VentasDiaDto(_hoy, 0m, 0));
        _ = dto.Pendientes.Should().Be(new VentasPendientesDto(0, 0m));
        _ = dto.PorMetodoPago.Should().OnlyContain(m => m.Monto == 0m);
        _ = dto.TopProductos.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_ShouldFailValidation_WhenHoyIsNotAroundTheCurrentDay()
    {
        CurrentUser.Admin = true;

        var result = await Sender.Send(new GetDashboardVentasQuery(_hoy.AddDays(-3)));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("GetDashboardVentasQuery.Hoy");
    }

    // Exact cash for the sale total, so the day tests do not depend on change
    private static (MetodoPago, decimal)[] Exacto(decimal total) => [(MetodoPago.Efectivo, total)];

    // Branch A: 20 by its base employee, 10 by a colleague, one parked sale; branch B: 50 and one parked sale; a third branch sells nothing
    private async Task<(Guid SucursalA, Guid EmpleadoA, Guid SucursalB, Guid SucursalSinVentas)> SeedDosSucursalesAsync()
    {
        var (sucursalA, empleadoA, loteA) = await SeedSucursalConLoteAsync("Producto A");
        var (sucursalB, empleadoB, loteB) = await SeedSucursalConLoteAsync("Producto B");
        var (sucursalSinVentas, _) = await _seeder.SeedSucursalAsync();
        var colegaA = await _seeder.SeedEmpleadoAsync(sucursalA);
        _ = await _seeder.SeedVentaCobradaAsync(sucursalA, empleadoA, loteA, 2m, [(MetodoPago.Efectivo, 20m)], fechaUtc: Utc(1));
        _ = await _seeder.SeedVentaCobradaAsync(sucursalA, colegaA, loteA, 1m, [(MetodoPago.Tarjeta, 10m)], fechaUtc: Utc(2));
        _ = await _seeder.SeedVentaCobradaAsync(sucursalB, empleadoB, loteB, 5m, [(MetodoPago.Transferencia, 50m)], fechaUtc: Utc(3));
        await _seeder.SeedVentaAsync(sucursalA, colegaA, loteA, 1m, EstadoVenta.Pendiente, Utc(1));
        await _seeder.SeedVentaAsync(sucursalB, empleadoB, loteB, 4m, EstadoVenta.Pendiente, Utc(1));
        return (sucursalA, empleadoA, sucursalB, sucursalSinVentas);
    }
}
