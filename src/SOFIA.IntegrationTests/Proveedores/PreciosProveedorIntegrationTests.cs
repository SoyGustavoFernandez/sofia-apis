using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Proveedores.Commands.RegistrarPrecioProveedor;
using SOFIA.Application.Proveedores.Queries.GetPreciosProveedor;
using SOFIA.Application.Ventas.Commands.CreateVenta;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Proveedores;

// Supplier price history against real SQL Server: registration, the read endpoint, tenant isolation and the cost a sale snapshots
public class PreciosProveedorIntegrationTests : BaseIntegrationTest
{
    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company, so the prices of one never leak into another
    public PreciosProveedorIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        _seeder = new PosCatalogSeeder(DbContext);
    }

    private async Task<ProveedorDistribuidor> SeedProveedorAsync()
    {
        var proveedor = ProveedorDistribuidor.Create($"Prov{Guid.NewGuid():N}"[..20], $"20{Random.Shared.NextInt64(100_000_000, 999_999_999)}", null, null).Value!;
        _ = DbContext.Proveedores.Add(proveedor);
        _ = await DbContext.SaveChangesAsync();
        return proveedor;
    }

    [Fact]
    public async Task Registrar_ShouldStoreTheNetCostAndTheQueryShouldReturnItAsCurrent()
    {
        var proveedor = await SeedProveedorAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto precio");

        var registro = await Sender.Send(new RegistrarPrecioProveedorCommand(proveedor.Id, medicamento.Id, 10m, 15m));
        var lista = await Sender.Send(new GetPreciosProveedorQuery(proveedor.Id));

        _ = registro.IsSuccess.Should().BeTrue();
        var fila = lista.Value!.Items.Should().ContainSingle().Subject;
        _ = fila.Id.Should().Be(registro.Value);
        _ = fila.ProductoId.Should().Be(medicamento.Id);
        _ = fila.ProductoNombre.Should().Be("Producto precio");
        _ = fila.CostoPorUnidadBase.Should().Be(8.5m);
        _ = fila.FechaFinVigencia.Should().BeNull();
        _ = fila.Vigente.Should().BeTrue();
        var persistido = await DbContext.HistorialPreciosProveedor.AsNoTracking().SingleAsync(h => h.Id == registro.Value);
        _ = persistido.TenantId.Should().Be(CurrentUser.Empresa);
    }

    [Fact]
    public async Task Registrar_ShouldCloseThePreviousPriceOfTheSameProducto_AndKeepItInTheHistory()
    {
        var proveedor = await SeedProveedorAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto historial");

        var primero = await Sender.Send(new RegistrarPrecioProveedorCommand(proveedor.Id, medicamento.Id, 10m, 15m));
        var segundo = await Sender.Send(new RegistrarPrecioProveedorCommand(proveedor.Id, medicamento.Id, 12m, 0m));
        var lista = await Sender.Send(new GetPreciosProveedorQuery(proveedor.Id));

        var filas = lista.Value!.Items.ToList();
        _ = filas.Select(f => f.Id).Should().Equal(segundo.Value, primero.Value);
        _ = filas[0].Vigente.Should().BeTrue();
        _ = filas[0].FechaFinVigencia.Should().BeNull();
        _ = filas[1].Vigente.Should().BeFalse();
        _ = filas[1].FechaFinVigencia.Should().Be(filas[0].FechaInicioVigencia);
        _ = filas[1].CostoPorUnidadBase.Should().Be(8.5m);
    }

    [Fact]
    public async Task Registrar_ShouldNotTouchThePricesOfAnotherProveedorForTheSameProducto()
    {
        var proveedor = await SeedProveedorAsync();
        var otroProveedor = await SeedProveedorAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto compartido");

        _ = await Sender.Send(new RegistrarPrecioProveedorCommand(otroProveedor.Id, medicamento.Id, 9m, 0m));
        _ = await Sender.Send(new RegistrarPrecioProveedorCommand(proveedor.Id, medicamento.Id, 10m, 0m));
        var deOtro = await Sender.Send(new GetPreciosProveedorQuery(otroProveedor.Id));

        var fila = deOtro.Value!.Items.Should().ContainSingle().Subject;
        _ = fila.Vigente.Should().BeTrue();
        _ = fila.FechaFinVigencia.Should().BeNull();
    }

    [Fact]
    public async Task Query_ShouldPaginateNewestFirstWithStableOrder()
    {
        var proveedor = await SeedProveedorAsync();
        var a = await _seeder.SeedMedicamentoAsync("Producto A");
        var b = await _seeder.SeedMedicamentoAsync("Producto B");
        var c = await _seeder.SeedMedicamentoAsync("Producto C");
        var ids = new List<Guid>();
        foreach (var producto in new[] { a, b, c })
        {
            ids.Add((await Sender.Send(new RegistrarPrecioProveedorCommand(proveedor.Id, producto.Id, 5m, 0m))).Value);
            await Task.Delay(20);
        }

        var primera = await Sender.Send(new GetPreciosProveedorQuery(proveedor.Id, 1, 2));
        var segunda = await Sender.Send(new GetPreciosProveedorQuery(proveedor.Id, 2, 2));

        _ = primera.Value!.TotalCount.Should().Be(3);
        _ = primera.Value!.TotalPages.Should().Be(2);
        _ = primera.Value!.Items.Select(f => f.Id).Should().Equal(ids[2], ids[1]);
        _ = segunda.Value!.Items.Select(f => f.Id).Should().Equal(ids[0]);
        _ = segunda.Value!.Items.Single().ProductoNombre.Should().Be("Producto A");
    }

    [Fact]
    public async Task Query_ShouldReturnNotFound_WhenTheProveedorBelongsToAnotherCompany()
    {
        var proveedor = await SeedProveedorAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto aislado");
        _ = await Sender.Send(new RegistrarPrecioProveedorCommand(proveedor.Id, medicamento.Id, 10m, 0m));

        CurrentUser.Empresa = Guid.NewGuid();
        var result = await Sender.Send(new GetPreciosProveedorQuery(proveedor.Id));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Proveedor.NotFound");
        _ = result.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task Registrar_ShouldReturnNotFoundAndStoreNothing_WhenTheProveedorOrTheProductoBelongToAnotherCompany()
    {
        var proveedorAjeno = await SeedProveedorAsync();
        var medicamentoAjeno = await _seeder.SeedMedicamentoAsync("Producto ajeno");
        CurrentUser.Empresa = Guid.NewGuid();
        var propio = await SeedProveedorAsync();
        var medicamentoPropio = await _seeder.SeedMedicamentoAsync("Producto propio");

        var proveedorFuera = await Sender.Send(new RegistrarPrecioProveedorCommand(proveedorAjeno.Id, medicamentoPropio.Id, 10m, 0m));
        var productoFuera = await Sender.Send(new RegistrarPrecioProveedorCommand(propio.Id, medicamentoAjeno.Id, 10m, 0m));

        _ = proveedorFuera.Error.Code.Should().Be("Proveedor.NotFound");
        _ = productoFuera.Error.Code.Should().Be("Medicamento.NotFound");
        _ = (await DbContext.HistorialPreciosProveedor.AsNoTracking().AnyAsync(h => h.ProveedorId == proveedorAjeno.Id || h.ProveedorId == propio.Id)).Should().BeFalse();
    }

    [Fact]
    public async Task CreateVenta_ShouldSnapshotTheCostOfTheCurrentSupplierPrice_AndIgnoreTheSupersededOne()
    {
        var proveedor = await SeedProveedorAsync();
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto costo");
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 100m);
        var sesion = await _seeder.SeedSesionAbiertaAsync(sucursalId, empleadoId);
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = empleadoId;

        var ventaSinPrecio = await VenderAsync(sesion.Id, lote.Id);
        _ = await Sender.Send(new RegistrarPrecioProveedorCommand(proveedor.Id, medicamento.Id, 10m, 15m));
        var ventaConPrimerPrecio = await VenderAsync(sesion.Id, lote.Id);
        _ = await Sender.Send(new RegistrarPrecioProveedorCommand(proveedor.Id, medicamento.Id, 12m, 0m));
        var ventaConSegundoPrecio = await VenderAsync(sesion.Id, lote.Id);

        _ = (await CostoDeLaVentaAsync(ventaSinPrecio)).Should().Be(0m, because: "there was no supplier price yet");
        _ = (await CostoDeLaVentaAsync(ventaConPrimerPrecio)).Should().Be(8.5m);
        _ = (await CostoDeLaVentaAsync(ventaConSegundoPrecio)).Should().Be(12m, because: "the first price was closed when the second was registered");
        _ = (await CostoDeLaVentaAsync(ventaConPrimerPrecio)).Should().Be(8.5m, because: "sales already made are not recalculated");
    }

    private async Task<Guid> VenderAsync(Guid sesionId, Guid loteId)
    {
        var venta = await Sender.Send(new CreateVentaCommand(null, sesionId, [new CreateVentaDetailDto(loteId, 1m)], [], EstadoVenta.Pendiente));
        _ = venta.IsSuccess.Should().BeTrue(because: venta.IsFailure ? venta.Error.Code : string.Empty);
        return venta.Value!.VentaId;
    }

    private Task<decimal> CostoDeLaVentaAsync(Guid ventaId) =>
        DbContext.Ventas.AsNoTracking().Where(v => v.Id == ventaId).SelectMany(v => v.Detalles).Select(d => d.CostoUnitarioHistorico).SingleAsync();
}
