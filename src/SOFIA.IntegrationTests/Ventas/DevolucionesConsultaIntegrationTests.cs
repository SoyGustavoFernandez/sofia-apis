using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Devoluciones.Queries.GetDevolucionById;
using SOFIA.Application.Devoluciones.Queries.GetDevoluciones;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Ventas;

// Listing and detail of returns against real SQL Server: the credit note number filter, the branch scope and the lines with their product data
public class DevolucionesConsultaIntegrationTests : BaseIntegrationTest
{
    private const string PrefijoNotaCredito = "BC01";
    private const int CorrelativoNotaCredito = 7;
    private const string NumeroNotaCredito = "BC01-00000007";

    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company, so the returns of one never leak into another
    public DevolucionesConsultaIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        _seeder = new PosCatalogSeeder(DbContext);
    }

    private sealed record Escenario(Guid SucursalId, Guid EmpleadoId, DevolucionCabecera Devolucion, Venta Venta, string Producto, string Lote);

    // A collected sale of 3 units at 10 with 2 units returned for 20 in cash, whose return already carries its credit note
    private async Task<Escenario> SeedAsync(DateTime? fechaUtc = null)
    {
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Paracetamol consulta");
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 100m);
        var sesion = await _seeder.SeedSesionAbiertaAsync(sucursalId, empleadoId);
        var venta = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, lote.Id, 3m, [(MetodoPago.Efectivo, 30m)], sesion.Id);
        var devolucion = await _seeder.SeedDevolucionAsync(venta, 2m, sesion.Id, 20m, MetodoPago.Efectivo, fechaUtc);

        var serieNc = SunatSerieFiscal.Create(sucursalId, TipoComprobante.NotaCredito, PrefijoNotaCredito, 1, SunatSerieFiscal.EstadoActiva).Value!;
        _ = DbContext.SUNATSeriesFiscales.Add(serieNc);
        var notaCredito = SunatComprobanteEmitido.Create(
            venta.Id, serieNc.Id, CorrelativoNotaCredito, "1", "12345678", "CLIENTE TEST",
            20m, 0, 0, 20m, null, EstadoComprobanteSunat.NoEnviado, null, null, null).Value!;
        _ = DbContext.SUNATComprobantesEmitidos.Add(notaCredito);
        _ = devolucion.Update(devolucion.ComprobanteOrigenId, notaCredito.Id, devolucion.EmpleadoAutorizaId, devolucion.MotivoSunatCatalogo, devolucion.SustentoDescriptivo, devolucion.FechaDevolucion);
        _ = await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = empleadoId;
        return new Escenario(sucursalId, empleadoId, devolucion, venta, "Paracetamol consulta", lote.NumeroLoteMfr);
    }

    private async Task<List<DevolucionResumenDto>> ListarAsync(GetDevolucionesQuery query) =>
        [.. (await Sender.Send(query)).Value!.Items];

    [Fact]
    public async Task GetDevoluciones_ShouldReturnTheEnrichedRow_WithNumbersNamesAndAmounts()
    {
        var e = await SeedAsync();

        var result = await Sender.Send(new GetDevolucionesQuery());

        _ = result.IsSuccess.Should().BeTrue();
        _ = result.Value!.TotalCount.Should().Be(1);
        var fila = result.Value.Items.Single();
        _ = fila.Id.Should().Be(e.Devolucion.Id);
        _ = fila.VentaId.Should().Be(e.Venta.Id, because: "the screen links to the sale, not to the receipt");
        _ = fila.CodigoVenta.Should().StartWith("B").And.EndWith("-00000001");
        _ = fila.NumeroNotaCredito.Should().Be(NumeroNotaCredito);
        _ = fila.ClienteNombre.Should().Be("Público General");
        _ = fila.EmpleadoAutorizaNombre.Should().Be("Ana Perez");
        _ = fila.SucursalNombre.Should().NotBeNullOrEmpty();
        _ = fila.MontoNotaCredito.Should().Be(20m);
        _ = fila.MontoReembolsado.Should().Be(20m);
        _ = fila.MetodoReembolso.Should().Be("Efectivo");
        _ = fila.MotivoSunatCatalogo.Should().Be("07");
    }

    [Theory]
    [InlineData("BC01-00000007", true)]
    [InlineData("bc01", true)]
    [InlineData("0007", true)]
    [InlineData("7", true)]
    [InlineData("BC01-7", true)]
    [InlineData("BC02", false)]
    [InlineData("BC01-00000008", false)]
    [InlineData("8", false)]
    public async Task GetDevoluciones_ShouldFilterByTheCreditNoteNumber_AsPrintedOrPartOfIt(string numero, bool coincide)
    {
        _ = await SeedAsync();

        var filas = await ListarAsync(new GetDevolucionesQuery { NumeroNotaCredito = numero });

        _ = filas.Count.Should().Be(coincide ? 1 : 0);
    }

    [Fact]
    public async Task GetDevoluciones_ShouldHideReturnsWithoutCreditNote_WhenFilteringByItsNumber()
    {
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Sin nota");
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 100m);
        var sesion = await _seeder.SeedSesionAbiertaAsync(sucursalId, empleadoId);
        var venta = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, lote.Id, 1m, [(MetodoPago.Efectivo, 10m)], sesion.Id);
        _ = await _seeder.SeedDevolucionAsync(venta, 1m, sesion.Id, 10m);
        CurrentUser.Sucursal = sucursalId;

        var sinFiltro = await ListarAsync(new GetDevolucionesQuery());
        var conFiltro = await ListarAsync(new GetDevolucionesQuery { NumeroNotaCredito = "BC01" });

        _ = sinFiltro.Should().ContainSingle().Which.NumeroNotaCredito.Should().BeNull(because: "old returns have no credit note");
        _ = conFiltro.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDevoluciones_ShouldApplyTheOtherFilters()
    {
        var e = await SeedAsync(new DateTime(2026, 11, 5, 15, 0, 0, DateTimeKind.Utc));

        _ = (await ListarAsync(new GetDevolucionesQuery { MetodoReembolso = MetodoPago.Efectivo })).Should().HaveCount(1);
        _ = (await ListarAsync(new GetDevolucionesQuery { MetodoReembolso = MetodoPago.YapePlin })).Should().BeEmpty();
        _ = (await ListarAsync(new GetDevolucionesQuery { MotivoSunatCatalogo = "07" })).Should().HaveCount(1);
        _ = (await ListarAsync(new GetDevolucionesQuery { MotivoSunatCatalogo = "06" })).Should().BeEmpty();
        _ = (await ListarAsync(new GetDevolucionesQuery { Destino = DestinoDevolucion.Reingreso_Venta })).Should().HaveCount(1);
        _ = (await ListarAsync(new GetDevolucionesQuery { Destino = DestinoDevolucion.Cuarentena_DIGEMID })).Should().BeEmpty();
        _ = (await ListarAsync(new GetDevolucionesQuery { MontoMin = 20m, MontoMax = 20m })).Should().HaveCount(1, because: "both ends are inclusive");
        _ = (await ListarAsync(new GetDevolucionesQuery { MontoMin = 20.01m })).Should().BeEmpty();
        _ = (await ListarAsync(new GetDevolucionesQuery { MontoMax = 19.99m })).Should().BeEmpty();
        _ = (await ListarAsync(new GetDevolucionesQuery { EmpleadoAutorizaId = e.EmpleadoId })).Should().HaveCount(1);
        _ = (await ListarAsync(new GetDevolucionesQuery { EmpleadoAutorizaId = Guid.NewGuid() })).Should().BeEmpty();
        // The SPA sends the start and the end instant of the chosen days, both inclusive
        var inicioDia = new DateTime(2026, 11, 5, 0, 0, 0, DateTimeKind.Utc);
        var finDia = new DateTime(2026, 11, 5, 23, 59, 59, DateTimeKind.Utc);
        _ = (await ListarAsync(new GetDevolucionesQuery { FechaInicio = inicioDia, FechaFin = finDia })).Should().HaveCount(1);
        _ = (await ListarAsync(new GetDevolucionesQuery { FechaInicio = new DateTime(2026, 11, 5, 15, 0, 0, DateTimeKind.Utc), FechaFin = new DateTime(2026, 11, 5, 15, 0, 0, DateTimeKind.Utc) })).Should().HaveCount(1, because: "both ends are inclusive");
        _ = (await ListarAsync(new GetDevolucionesQuery { FechaInicio = finDia.AddSeconds(1) })).Should().BeEmpty();
        _ = (await ListarAsync(new GetDevolucionesQuery { FechaFin = inicioDia.AddSeconds(-1) })).Should().BeEmpty();
    }

    [Fact]
    public async Task GetDevoluciones_ShouldPageAndCountTheSameRows_AndOrderTheNewestFirst()
    {
        var (sucursalId, empleadoId) = await _seeder.SeedSucursalAsync();
        var medicamento = await _seeder.SeedMedicamentoAsync("Paginado");
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 100m);
        var sesion = await _seeder.SeedSesionAbiertaAsync(sucursalId, empleadoId);
        var ids = new List<Guid>();
        for (var dia = 1; dia <= 3; dia++)
        {
            var venta = await _seeder.SeedVentaCobradaAsync(sucursalId, empleadoId, lote.Id, 1m, [(MetodoPago.Efectivo, 10m)], sesion.Id);
            ids.Add((await _seeder.SeedDevolucionAsync(venta, 1m, sesion.Id, 10m, MetodoPago.Efectivo, new DateTime(2026, 11, dia, 12, 0, 0, DateTimeKind.Utc))).Id);
        }

        CurrentUser.Sucursal = sucursalId;

        var primera = (await Sender.Send(new GetDevolucionesQuery { PageNumber = 1, PageSize = 2 })).Value!;
        var segunda = (await Sender.Send(new GetDevolucionesQuery { PageNumber = 2, PageSize = 2 })).Value!;

        _ = primera.TotalCount.Should().Be(3);
        _ = primera.TotalPages.Should().Be(2);
        _ = primera.Items.Select(d => d.Id).Should().Equal(ids[2], ids[1]);
        _ = segunda.Items.Select(d => d.Id).Should().Equal(ids[0]);
    }

    [Fact]
    public async Task GetDevoluciones_ShouldHideAndRejectReturnsOfABranchTheCallerCannotAccess()
    {
        var e = await SeedAsync();
        var (otraSucursalId, _) = await _seeder.SeedSucursalAsync();
        CurrentUser.Sucursal = otraSucursalId;

        var lista = await Sender.Send(new GetDevolucionesQuery());
        var detalle = await Sender.Send(new GetDevolucionByIdQuery(e.Devolucion.Id));

        _ = lista.Value!.TotalCount.Should().Be(0);
        _ = detalle.IsFailure.Should().BeTrue();
        _ = detalle.StatusCode.Should().Be(404, because: "a branch must not learn that a return of another one exists");
        _ = detalle.Error.Code.Should().Be("Devolucion.NotFound");
    }

    [Fact]
    public async Task GetDevolucionById_ShouldReturnTheHeaderAndItsLinesWithProductData()
    {
        var e = await SeedAsync();

        var result = await Sender.Send(new GetDevolucionByIdQuery(e.Devolucion.Id));

        _ = result.IsSuccess.Should().BeTrue();
        var d = result.Value!;
        _ = d.NumeroNotaCredito.Should().Be(NumeroNotaCredito);
        _ = d.SustentoDescriptivo.Should().Be("Devolucion de prueba");
        _ = d.MontoReembolsado.Should().Be(20m);
        var linea = d.Lineas.Should().ContainSingle().Subject;
        _ = linea.ProductoNombre.Should().Be(e.Producto);
        _ = linea.NumeroLote.Should().Be(e.Lote);
        _ = linea.Presentacion.Should().BeNull();
        _ = linea.CantidadDevuelta.Should().Be(2m);
        _ = linea.PrecioUnitario.Should().Be(10m);
        _ = linea.Subtotal.Should().Be(20m);
        _ = linea.Destino.Should().Be("Reingreso_Venta");
    }

    [Fact]
    public async Task GetDevolucionById_ShouldKeepTheLineAndItsProductName_WhenTheProductWasDeletedAfterTheSale()
    {
        var e = await SeedAsync();
        var lote = await DbContext.LotesInventario.IgnoreQueryFilters().SingleAsync(l => l.NumeroLoteMfr == e.Lote);
        _ = await DbContext.Database.ExecuteSqlAsync($"UPDATE Medicamentos SET IsDeleted = 1 WHERE Producto_ID = {lote.ProductoId}");
        DbContext.ChangeTracker.Clear();

        var result = await Sender.Send(new GetDevolucionByIdQuery(e.Devolucion.Id));

        _ = result.IsSuccess.Should().BeTrue();
        var linea = result.Value!.Lineas.Should().ContainSingle(because: "history must not lose a returned line").Subject;
        _ = linea.ProductoNombre.Should().Be(e.Producto, because: "the history shows what was sold even if the product was removed later");
        _ = linea.NumeroLote.Should().Be(e.Lote);
    }

    [Fact]
    public async Task GetDevolucionById_ShouldReturnNotFound_WhenTheReturnDoesNotExist()
    {
        _ = await SeedAsync();

        var result = await Sender.Send(new GetDevolucionByIdQuery(Guid.NewGuid()));

        _ = result.StatusCode.Should().Be(404);
        _ = result.Error.Code.Should().Be("Devolucion.NotFound");
    }
}
