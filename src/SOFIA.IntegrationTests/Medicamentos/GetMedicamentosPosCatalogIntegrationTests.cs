using SOFIA.Application.Medicamentos.Queries.GetMedicamentos;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Medicamentos;

public class GetMedicamentosPosCatalogIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    // Each test filters by its own name token so products seeded by other tests never leak into the page.
    private readonly string _token = $"Pos{Guid.NewGuid():N}"[..12];

    [Fact]
    public async Task Handle_ShouldReturnWholeCatalogAlphabetically_WhenTheBranchHasNoSales()
    {
        var seeder = new PosCatalogSeeder(DbContext);
        var (sucursalId, _) = await seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        _ = await seeder.SeedMedicamentoAsync($"{_token} C");
        _ = await seeder.SeedMedicamentoAsync($"{_token} A");
        _ = await seeder.SeedMedicamentoAsync($"{_token} B");

        var result = await Sender.Send(new GetMedicamentosQuery { NombreComercial = _token, OrdenarPorMasVendidos = true, PageSize = 2 });

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.TotalCount.Should().Be(3);
        _ = result.Value!.TotalPages.Should().Be(2);
        _ = result.Value!.Items.Select(x => x.NombreComercial).Should().Equal($"{_token} A", $"{_token} B");
    }

    [Fact]
    public async Task Handle_ShouldPutBestSellersFirstThenTheRestAlphabetically_WhenTheBranchHasSales()
    {
        var seeder = new PosCatalogSeeder(DbContext);
        var (sucursalId, empleadoId) = await seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        var a = await seeder.SeedMedicamentoAsync($"{_token} A");
        var b = await seeder.SeedMedicamentoAsync($"{_token} B");
        _ = await seeder.SeedMedicamentoAsync($"{_token} D");
        _ = await seeder.SeedMedicamentoAsync($"{_token} C");
        var loteA = await seeder.SeedLoteAsync(a.Id, sucursalId, 50m);
        var loteB = await seeder.SeedLoteAsync(b.Id, sucursalId, 50m);
        await seeder.SeedVentaAsync(sucursalId, empleadoId, loteA.Id, 2m);
        await seeder.SeedVentaAsync(sucursalId, empleadoId, loteB.Id, 3m);
        await seeder.SeedVentaAsync(sucursalId, empleadoId, loteB.Id, 4m);

        var result = await Sender.Send(new GetMedicamentosQuery { NombreComercial = _token, OrdenarPorMasVendidos = true, PageSize = 12 });

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.TotalCount.Should().Be(4);
        _ = result.Value!.Items.Select(x => x.NombreComercial).Should().Equal($"{_token} B", $"{_token} A", $"{_token} C", $"{_token} D");
    }

    [Fact]
    public async Task Handle_ShouldPaginateTheRankedCatalog_WhenASecondPageIsRequested()
    {
        var seeder = new PosCatalogSeeder(DbContext);
        var (sucursalId, empleadoId) = await seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        _ = await seeder.SeedMedicamentoAsync($"{_token} A");
        _ = await seeder.SeedMedicamentoAsync($"{_token} B");
        var c = await seeder.SeedMedicamentoAsync($"{_token} C");
        var loteC = await seeder.SeedLoteAsync(c.Id, sucursalId, 50m);
        await seeder.SeedVentaAsync(sucursalId, empleadoId, loteC.Id, 1m);

        var result = await Sender.Send(new GetMedicamentosQuery { NombreComercial = _token, OrdenarPorMasVendidos = true, PageNumber = 2, PageSize = 2 });

        _ = result.Value!.TotalCount.Should().Be(3);
        _ = result.Value!.Items.Select(x => x.NombreComercial).Should().Equal($"{_token} B");
    }

    [Fact]
    public async Task Handle_ShouldIgnoreSalesThatDoNotCount_WhenTheyAreFromAnotherBranchOldOrNotCompleted()
    {
        var seeder = new PosCatalogSeeder(DbContext);
        var (sucursalId, empleadoId) = await seeder.SeedSucursalAsync();
        var (otraSucursalId, otroEmpleadoId) = await seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        _ = await seeder.SeedMedicamentoAsync($"{_token} A");
        var b = await seeder.SeedMedicamentoAsync($"{_token} B");
        var c = await seeder.SeedMedicamentoAsync($"{_token} C");
        var d = await seeder.SeedMedicamentoAsync($"{_token} D");
        var e = await seeder.SeedMedicamentoAsync($"{_token} E");
        var loteB = await seeder.SeedLoteAsync(b.Id, otraSucursalId, 50m);
        var loteC = await seeder.SeedLoteAsync(c.Id, sucursalId, 50m);
        var loteD = await seeder.SeedLoteAsync(d.Id, sucursalId, 50m);
        var loteE = await seeder.SeedLoteAsync(e.Id, sucursalId, 50m);
        await seeder.SeedVentaAsync(otraSucursalId, otroEmpleadoId, loteB.Id, 9m);
        await seeder.SeedVentaAsync(sucursalId, empleadoId, loteC.Id, 9m, fechaUtc: DateTime.UtcNow.AddDays(-31));
        await seeder.SeedVentaAsync(sucursalId, empleadoId, loteD.Id, 9m, EstadoVenta.Anulada);
        await seeder.SeedVentaAsync(sucursalId, empleadoId, loteD.Id, 9m, EstadoVenta.Pendiente);
        await seeder.SeedVentaAsync(sucursalId, empleadoId, loteE.Id, 1m);

        var result = await Sender.Send(new GetMedicamentosQuery { NombreComercial = _token, OrdenarPorMasVendidos = true, PageSize = 12 });

        _ = result.Value!.Items.Select(x => x.NombreComercial).Should().Equal($"{_token} E", $"{_token} A", $"{_token} B", $"{_token} C", $"{_token} D");
    }

    [Fact]
    public async Task Handle_ShouldSkipASoftDeletedBestSellerWithoutShrinkingThePage_WhenRankingBySales()
    {
        var seeder = new PosCatalogSeeder(DbContext);
        var (sucursalId, empleadoId) = await seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        _ = await seeder.SeedMedicamentoAsync($"{_token} A");
        _ = await seeder.SeedMedicamentoAsync($"{_token} B");
        _ = await seeder.SeedMedicamentoAsync($"{_token} C");
        var borrado = await seeder.SeedMedicamentoAsync($"{_token} Z");
        var lote = await seeder.SeedLoteAsync(borrado.Id, sucursalId, 50m);
        await seeder.SeedVentaAsync(sucursalId, empleadoId, lote.Id, 9m);
        borrado.IsDeleted = true;
        _ = await DbContext.SaveChangesAsync();

        var result = await Sender.Send(new GetMedicamentosQuery { NombreComercial = _token, OrdenarPorMasVendidos = true, PageSize = 2 });

        _ = result.Value!.TotalCount.Should().Be(3);
        _ = result.Value!.Items.Select(x => x.NombreComercial).Should().Equal($"{_token} A", $"{_token} B");
    }

    [Fact]
    public async Task Handle_ShouldCountOnlySellableStockOfTheOwnBranch_WhenThePosFlagIsSet()
    {
        var seeder = new PosCatalogSeeder(DbContext);
        var (sucursalId, empleadoId) = await seeder.SeedSucursalAsync();
        var (otraSucursalId, _) = await seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        var producto = await seeder.SeedMedicamentoAsync($"{_token} A");
        var soloOtraSucursal = await seeder.SeedMedicamentoAsync($"{_token} B");
        _ = await seeder.SeedLoteAsync(producto.Id, sucursalId, 5m);
        _ = await seeder.SeedLoteAsync(producto.Id, otraSucursalId, 100m);
        _ = await seeder.SeedLoteAsync(producto.Id, sucursalId, 3m, PosCatalogSeeder.CaducidadIniciadaHace(23));
        _ = await seeder.SeedLoteAsync(producto.Id, sucursalId, 30m, PosCatalogSeeder.CaducidadIniciadaHace(25));
        var retenido = await seeder.SeedLoteAsync(producto.Id, sucursalId, 70m);
        await seeder.SeedCuarentenaAsync(sucursalId, retenido.Id, empleadoId, EstadoResolucionCuarentena.Retenido);
        _ = await seeder.SeedLoteAsync(soloOtraSucursal.Id, otraSucursalId, 40m);

        var pos = await Sender.Send(new GetMedicamentosQuery { NombreComercial = _token, IncluirStock = true, SoloStockVendibleEnMiSucursal = true });
        var general = await Sender.Send(new GetMedicamentosQuery { NombreComercial = _token, IncluirStock = true });

        _ = pos.Value!.Items.Select(x => x.StockTotal).Should().Equal(8m, 0m);
        _ = general.Value!.Items.Select(x => x.StockTotal).Should().Equal(208m, 40m);
    }

    [Fact]
    public async Task Handle_ShouldFail_WhenRankingBySalesWithoutABranchInTheToken()
    {
        var result = await Sender.Send(new GetMedicamentosQuery { NombreComercial = _token, OrdenarPorMasVendidos = true });

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Auth.Sucursal");
    }
}
