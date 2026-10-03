using SOFIA.Application.Dashboard.Queries.GetDashboardCuarentena;
using SOFIA.Application.Dashboard.Queries.GetDashboardInventario;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Dashboard;

public class DashboardInventarioIntegrationTests : BaseIntegrationTest
{
    private readonly DateTimeOffset _hoy = DashboardTestClock.HoyLima();
    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company, so the figures of one never leak into another
    public DashboardInventarioIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        _seeder = new PosCatalogSeeder(DbContext);
    }

    [Fact]
    public async Task Inventario_ShouldBucketLotsWithStockByExpiryDay_WhenLotsExpireAroundEachBoundary()
    {
        var (sucursalId, _) = await _seeder.SeedSucursalAsync();
        var (otraSucursalId, _) = await _seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        var producto = await _seeder.SeedMedicamentoAsync("Producto vencimientos");
        foreach (var dias in new[] { -30, -1, 0, 30, 31, 60, 61, 90, 91 })
        {
            _ = await _seeder.SeedLoteAsync(producto.Id, sucursalId, 5m, _hoy.AddDays(dias));
        }

        _ = await _seeder.SeedLoteAsync(producto.Id, sucursalId, 0m, _hoy.AddDays(-1));
        _ = await _seeder.SeedLoteAsync(producto.Id, sucursalId, 0m, _hoy.AddDays(10));
        _ = await _seeder.SeedLoteAsync(producto.Id, otraSucursalId, 5m, _hoy.AddDays(-1));
        _ = await _seeder.SeedLoteAsync(producto.Id, otraSucursalId, 5m, _hoy.AddDays(10));

        var result = await Sender.Send(new GetDashboardInventarioQuery(_hoy));

        _ = result.IsSuccess.Should().BeTrue();
        var dto = result.Value!;
        _ = dto.LotesVencidosConStock.Should().Be(2, because: "a lot whose expiry day is today still sells");
        _ = dto.LotesVencen30.Should().Be(2);
        _ = dto.LotesVencen60.Should().Be(2);
        _ = dto.LotesVencen90.Should().Be(2);
    }

    [Fact]
    public async Task Inventario_ShouldCountLowAndOutOfStockFromSellableStock_WhenTheUserSeesOneBranch()
    {
        var (SucursalA, _) = await SeedAlertasDeStockAsync();
        CurrentUser.Sucursal = SucursalA;

        var inventario = await Sender.Send(new GetDashboardInventarioQuery(_hoy));
        var cuarentena = await Sender.Send(new GetDashboardCuarentenaQuery());

        var dto = inventario.Value!;
        _ = dto.UmbralStockBajo.Should().Be(10m);
        _ = dto.ProductosStockBajo.Should().Be(2, because: "exactly the threshold is low and quarantined units are not sellable");
        _ = dto.ProductosSinStock.Should().Be(3, because: "empty, expired-only and quarantined-only products have nothing to sell");
        _ = dto.LotesVencidosConStock.Should().Be(1);
        _ = cuarentena.Value.Should().Be(new DashboardCuarentenaDto(2));
    }

    [Fact]
    public async Task Inventario_ShouldCountEachBranchAndProductPair_WhenTheUserIsAdmin()
    {
        _ = await SeedAlertasDeStockAsync();
        CurrentUser.Admin = true;

        var inventario = await Sender.Send(new GetDashboardInventarioQuery(_hoy));
        var cuarentena = await Sender.Send(new GetDashboardCuarentenaQuery());

        _ = inventario.Value!.ProductosStockBajo.Should().Be(3);
        _ = inventario.Value.ProductosSinStock.Should().Be(4);
        _ = cuarentena.Value.Should().Be(new DashboardCuarentenaDto(3));
    }

    [Fact]
    public async Task Inventario_ShouldReturnZeros_WhenTheCompanyHasNoData()
    {
        CurrentUser.Admin = true;

        var inventario = await Sender.Send(new GetDashboardInventarioQuery(_hoy));
        var cuarentena = await Sender.Send(new GetDashboardCuarentenaQuery());

        _ = inventario.Value.Should().Be(new DashboardInventarioDto(0, 0, 0, 0, 0, 0, 10m));
        _ = cuarentena.Value.Should().Be(new DashboardCuarentenaDto(0));
    }

    [Fact]
    public async Task Inventario_ShouldReturnZeros_WhenTheUserHasNoBranchInScope()
    {
        _ = await SeedAlertasDeStockAsync();

        var inventario = await Sender.Send(new GetDashboardInventarioQuery(_hoy));
        var cuarentena = await Sender.Send(new GetDashboardCuarentenaQuery());

        _ = inventario.Value.Should().Be(new DashboardInventarioDto(0, 0, 0, 0, 0, 0, 10m));
        _ = cuarentena.Value.Should().Be(new DashboardCuarentenaDto(0));
    }

    [Fact]
    public async Task Inventario_ShouldFailValidation_WhenHoyIsNotAroundTheCurrentDay()
    {
        CurrentUser.Admin = true;

        var result = await Sender.Send(new GetDashboardInventarioQuery(default));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("GetDashboardInventarioQuery.Hoy");
    }

    // Branch A: low = P1 (6 + 4) and P5 (3 next to a quarantined lot); out = P3 (empty), P4 (expired only), P6 (quarantined only); fine = P2 (11) and P7 (released)
    private async Task<(Guid SucursalA, Guid SucursalB)> SeedAlertasDeStockAsync()
    {
        var (sucursalA, empleadoA) = await _seeder.SeedSucursalAsync();
        var (sucursalB, empleadoB) = await _seeder.SeedSucursalAsync();
        var p1 = await _seeder.SeedMedicamentoAsync("P1 justo en el umbral");
        var p2 = await _seeder.SeedMedicamentoAsync("P2 sobre el umbral");
        var p3 = await _seeder.SeedMedicamentoAsync("P3 sin unidades");
        var p4 = await _seeder.SeedMedicamentoAsync("P4 solo vencido");
        var p5 = await _seeder.SeedMedicamentoAsync("P5 parte en cuarentena");
        var p6 = await _seeder.SeedMedicamentoAsync("P6 todo en cuarentena");
        var p7 = await _seeder.SeedMedicamentoAsync("P7 cuarentena liberada");
        var p8 = await _seeder.SeedMedicamentoAsync("P8 solo en la otra sucursal");

        _ = await _seeder.SeedLoteAsync(p1.Id, sucursalA, 6m);
        _ = await _seeder.SeedLoteAsync(p1.Id, sucursalA, 4m);
        _ = await _seeder.SeedLoteAsync(p2.Id, sucursalA, 11m);
        _ = await _seeder.SeedLoteAsync(p3.Id, sucursalA, 0m);
        _ = await _seeder.SeedLoteAsync(p4.Id, sucursalA, 50m, _hoy.AddDays(-1));
        _ = await _seeder.SeedLoteAsync(p5.Id, sucursalA, 3m);
        var p5Retenido = await _seeder.SeedLoteAsync(p5.Id, sucursalA, 40m);
        var p6Retenido = await _seeder.SeedLoteAsync(p6.Id, sucursalA, 40m);
        var p7Liberado = await _seeder.SeedLoteAsync(p7.Id, sucursalA, 30m);
        await _seeder.SeedCuarentenaAsync(sucursalA, p5Retenido.Id, empleadoA, EstadoResolucionCuarentena.Retenido);
        await _seeder.SeedCuarentenaAsync(sucursalA, p6Retenido.Id, empleadoA, EstadoResolucionCuarentena.Retenido);
        await _seeder.SeedCuarentenaAsync(sucursalA, p6Retenido.Id, empleadoA, EstadoResolucionCuarentena.Retenido);
        await _seeder.SeedCuarentenaAsync(sucursalA, p7Liberado.Id, empleadoA, EstadoResolucionCuarentena.Liberado);

        // Branch B: P8 is low (2), P1 has an empty lot row and one more lot is retained
        var p8Retenido = await _seeder.SeedLoteAsync(p8.Id, sucursalB, 20m);
        _ = await _seeder.SeedLoteAsync(p8.Id, sucursalB, 2m);
        _ = await _seeder.SeedLoteAsync(p1.Id, sucursalB, 0m);
        await _seeder.SeedCuarentenaAsync(sucursalB, p8Retenido.Id, empleadoB, EstadoResolucionCuarentena.Retenido);

        return (sucursalA, sucursalB);
    }
}
