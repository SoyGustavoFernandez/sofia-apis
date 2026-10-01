using SOFIA.Application.Ventas.Queries.GetLotesDisponiblesVenta;
using SOFIA.Application.Ventas.Queries.GetPresentacionesProductoVenta;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Ventas;

public class PosLookupsIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    private readonly string _token = $"Pos{Guid.NewGuid():N}"[..12];

    [Fact]
    public async Task GetLotesDisponibles_ShouldReturnOnlySellableLotsOfTheOwnBranch_OrderedByExpiry()
    {
        var seeder = new PosCatalogSeeder(DbContext);
        var (sucursalId, empleadoId) = await seeder.SeedSucursalAsync();
        var (otraSucursalId, _) = await seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        var producto = await seeder.SeedMedicamentoAsync($"{_token} A");
        var otroProducto = await seeder.SeedMedicamentoAsync($"{_token} B");
        var tardio = await seeder.SeedLoteAsync(producto.Id, sucursalId, 5m, DateTimeOffset.UtcNow.AddMonths(8));
        var proximo = await seeder.SeedLoteAsync(producto.Id, sucursalId, 7m, DateTimeOffset.UtcNow.AddMonths(2));
        _ = await seeder.SeedLoteAsync(producto.Id, otraSucursalId, 100m);
        var venceHoy = await seeder.SeedLoteAsync(producto.Id, sucursalId, 4m, PosCatalogSeeder.CaducidadIniciadaHace(23));
        _ = await seeder.SeedLoteAsync(producto.Id, sucursalId, 30m, PosCatalogSeeder.CaducidadIniciadaHace(25));
        _ = await seeder.SeedLoteAsync(producto.Id, sucursalId, 0m);
        var retenido = await seeder.SeedLoteAsync(producto.Id, sucursalId, 70m);
        await seeder.SeedCuarentenaAsync(sucursalId, retenido.Id, empleadoId, EstadoResolucionCuarentena.Retenido);
        var liberado = await seeder.SeedLoteAsync(producto.Id, sucursalId, 9m, DateTimeOffset.UtcNow.AddMonths(10));
        await seeder.SeedCuarentenaAsync(sucursalId, liberado.Id, empleadoId, EstadoResolucionCuarentena.Liberado);
        _ = await seeder.SeedLoteAsync(otroProducto.Id, sucursalId, 11m);

        var result = await Sender.Send(new GetLotesDisponiblesVentaQuery { ProductoId = producto.Id });

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Select(x => x.LoteId).Should().Equal(venceHoy.Id, proximo.Id, tardio.Id, liberado.Id);
        _ = result.Value![1].CantidadFisica.Should().Be(7m);
        _ = result.Value![1].ProductoNombre.Should().Be($"{_token} A");
        _ = result.Value![1].PrecioVentaBase.Should().Be(10m);
    }

    [Fact]
    public async Task GetLotesDisponibles_ShouldMatchByProductName_WhenNoIdIsGiven()
    {
        var seeder = new PosCatalogSeeder(DbContext);
        var (sucursalId, _) = await seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = sucursalId;
        var producto = await seeder.SeedMedicamentoAsync($"{_token} A");
        var lote = await seeder.SeedLoteAsync(producto.Id, sucursalId, 5m);

        var result = await Sender.Send(new GetLotesDisponiblesVentaQuery { ProductoNombre = _token.ToUpperInvariant() });

        _ = result.Value!.Select(x => x.LoteId).Should().Equal(lote.Id);
    }

    [Fact]
    public async Task GetLotesDisponibles_ShouldFail_WhenNoProductIsGiven()
    {
        CurrentUser.Sucursal = Guid.NewGuid();

        var result = await Sender.Send(new GetLotesDisponiblesVentaQuery());

        _ = result.Error.Code.Should().Be("Venta.LotesDisponibles.ProductoRequerido");
    }

    [Fact]
    public async Task GetLotesDisponibles_ShouldFail_WhenTheTokenHasNoBranch()
    {
        var result = await Sender.Send(new GetLotesDisponiblesVentaQuery { ProductoId = Guid.NewGuid() });

        _ = result.Error.Code.Should().Be("Auth.Sucursal");
    }

    [Fact]
    public async Task GetPresentaciones_ShouldReturnOnlyThePresentationsOfTheProduct()
    {
        var seeder = new PosCatalogSeeder(DbContext);
        var producto = await seeder.SeedMedicamentoAsync($"{_token} A");
        var otroProducto = await seeder.SeedMedicamentoAsync($"{_token} B");
        var caja = await seeder.SeedPresentacionAsync(producto.Id, "Caja x10", 10m, 45m);
        var blister = await seeder.SeedPresentacionAsync(producto.Id, "Blister x4", 4m, 20m);
        _ = await seeder.SeedPresentacionAsync(otroProducto.Id, "Caja x20", 20m, 80m);

        var result = await Sender.Send(new GetPresentacionesProductoVentaQuery(producto.Id));

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.Select(x => x.Id).Should().Equal(blister.Id, caja.Id);
        _ = result.Value![1].CantidadUnidadesBase.Should().Be(10m);
        _ = result.Value![1].PrecioVenta.Should().Be(45m);
    }
}
