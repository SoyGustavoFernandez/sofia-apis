using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Dashboard.Queries.GetDashboardCaja;
using SOFIA.Application.POS.Commands.CerrarCaja;
using SOFIA.Application.POS.Common;
using SOFIA.Application.Ventas.Commands.AnularVenta;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Ventas;

// Voiding a charged sale against real SQL Server: the caller's open drawer is required and the cash is counted in the drawer it left
public class AnularVentaCajaIntegrationTests : BaseIntegrationTest
{
    private readonly PosCatalogSeeder _seeder;

    // Every test runs in its own company, so the sessions of one never leak into another
    public AnularVentaCajaIntegrationTests(SofiaWebAppFactory factory) : base(factory)
    {
        CurrentUser.Empresa = Guid.NewGuid();
        _seeder = new PosCatalogSeeder(DbContext);
    }

    private async Task<(Guid SucursalId, Guid CajeroId, Guid AdminId, Guid LoteId)> SeedSucursalAsync()
    {
        var (sucursalId, cajeroId) = await _seeder.SeedSucursalAsync();
        var adminId = await _seeder.SeedEmpleadoAsync(sucursalId);
        var medicamento = await _seeder.SeedMedicamentoAsync("Producto anulacion");
        var lote = await _seeder.SeedLoteAsync(medicamento.Id, sucursalId, 500m);
        return (sucursalId, cajeroId, adminId, lote.Id);
    }

    private Task<decimal> EsperadoAsync(PosSesionCaja sesion) => ArqueoCajaCalculator.CalcularEfectivoEsperadoAsync(DbContext, sesion, CancellationToken.None);

    private Task<Venta> LeerVentaAsync(Guid ventaId) => DbContext.Ventas.AsNoTracking().Include(v => v.SesionAnulacion).SingleAsync(v => v.Id == ventaId);

    private Task<decimal> StockAsync(Guid loteId) => DbContext.LotesEnSucursal.AsNoTracking().Where(i => i.LoteId == loteId).Select(i => i.CantidadFisica).SingleAsync();

    [Fact]
    public async Task AnularVenta_ShouldBeRejectedAndChangeNothing_WhenTheSaleWasChargedAndTheCallerHasNoOpenSession()
    {
        var (sucursalId, cajeroId, adminId, loteId) = await SeedSucursalAsync();
        var sesionCajero = await _seeder.SeedSesionAbiertaAsync(sucursalId, cajeroId);
        var venta = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 1m, [(MetodoPago.Efectivo, 20m)], sesionCajero.Id);
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = adminId;

        var result = await Sender.Send(new AnularVentaCommand(venta.Id, "Error de cobro"));

        _ = result.IsFailure.Should().BeTrue();
        _ = result.Error.Code.Should().Be("Venta.Anular.SinSesionAbierta", because: "the cashier's open session is not the caller's own");
        var persistida = await LeerVentaAsync(venta.Id);
        _ = persistida.Estado.Should().Be(EstadoVenta.Completada);
        _ = persistida.SesionAnulacionId.Should().BeNull();
        _ = (await StockAsync(loteId)).Should().Be(500m);
        _ = (await EsperadoAsync(sesionCajero)).Should().Be(110m);
    }

    [Fact]
    public async Task AnularVenta_ShouldTakeTheCashFromTheCallersDrawer_WhenTheCollectingSessionIsAlreadyClosed()
    {
        var (sucursalId, cajeroId, adminId, loteId) = await SeedSucursalAsync();
        var sesionCajero = await _seeder.SeedSesionAbiertaAsync(sucursalId, cajeroId);
        var venta = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 1m, [(MetodoPago.Efectivo, 20m)], sesionCajero.Id); // 20 in, 10 change out
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = cajeroId;
        _ = (await Sender.Send(new CerrarCajaCommand(sesionCajero.Id, 110m))).IsSuccess.Should().BeTrue();
        CurrentUser.Empleado = adminId;
        var sesionAdmin = await _seeder.SeedSesionAbiertaAsync(sucursalId, adminId, 50m);

        var result = await Sender.Send(new AnularVentaCommand(venta.Id, "Error de cobro"));

        _ = result.IsSuccess.Should().BeTrue();
        var persistida = await LeerVentaAsync(venta.Id);
        _ = persistida.Estado.Should().Be(EstadoVenta.Anulada);
        _ = persistida.SesionId.Should().Be(sesionCajero.Id);
        _ = persistida.SesionAnulacionId.Should().Be(sesionAdmin.Id);
        _ = persistida.SesionAnulacion!.EmpleadoId.Should().Be(adminId);
        _ = (await StockAsync(loteId)).Should().Be(501m);
        _ = (await EsperadoAsync(sesionAdmin)).Should().Be(40m);
        _ = (await EsperadoAsync(sesionCajero)).Should().Be(110m, because: "the closed count already included the sale it collected");
        var cerrada = await DbContext.POSSesionesCaja.AsNoTracking().SingleAsync(s => s.Id == sesionCajero.Id);
        _ = cerrada.MontoCierreCalculado.Should().Be(110m);
        var dashboard = await Sender.Send(new GetDashboardCajaQuery());
        _ = dashboard.Value!.MiSesion!.EfectivoEsperado.Should().Be(40m);
    }

    [Fact]
    public async Task AnularVenta_ShouldLeaveTheDrawerAsBeforeTheSale_WhenVoidedFromTheSessionThatCollectedIt()
    {
        var (sucursalId, cajeroId, _, loteId) = await SeedSucursalAsync();
        var sesion = await _seeder.SeedSesionAbiertaAsync(sucursalId, cajeroId);
        var venta = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 1m, [(MetodoPago.Efectivo, 20m)], sesion.Id);
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = cajeroId;
        _ = (await EsperadoAsync(sesion)).Should().Be(110m);

        var result = await Sender.Send(new AnularVentaCommand(venta.Id, "Error de cobro"));

        _ = result.IsSuccess.Should().BeTrue();
        _ = (await LeerVentaAsync(venta.Id)).SesionAnulacionId.Should().Be(sesion.Id);
        _ = (await EsperadoAsync(sesion)).Should().Be(100m);
    }

    [Fact]
    public async Task AnularVenta_ShouldNeedNoSession_WhenTheSaleIsParked()
    {
        var (sucursalId, cajeroId, adminId, loteId) = await SeedSucursalAsync();
        var parked = Venta.Create(sucursalId, cajeroId, null, null, [DetalleVenta.Create(loteId, 1m, 10m, 8m).Value!], EstadoVenta.Pendiente).Value!;
        _ = DbContext.Ventas.Add(parked);
        _ = await DbContext.SaveChangesAsync();
        CurrentUser.Sucursal = sucursalId;
        CurrentUser.Empleado = adminId;

        var result = await Sender.Send(new AnularVentaCommand(parked.Id, "Cliente nunca volvio"));

        _ = result.IsSuccess.Should().BeTrue();
        var persistida = await LeerVentaAsync(parked.Id);
        _ = persistida.Estado.Should().Be(EstadoVenta.Anulada);
        _ = persistida.SesionAnulacionId.Should().BeNull();
    }

    [Fact]
    public async Task Arqueo_ShouldCountEachVoidInTheDrawerTheCashLeft_WhenSalesAreVoidedAcrossSessions()
    {
        var (sucursalId, cajeroId, adminId, loteId) = await SeedSucursalAsync();
        var sesionCajero = await _seeder.SeedSesionAbiertaAsync(sucursalId, cajeroId);
        var sesionAdmin = await _seeder.SeedSesionAbiertaAsync(sucursalId, adminId, 200m);
        var efectivo = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 1m, [(MetodoPago.Efectivo, 20m)], sesionCajero.Id); // net cash 10
        var mixta = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 2m, [(MetodoPago.YapePlin, 15m), (MetodoPago.Efectivo, 10m)], sesionCajero.Id); // net cash 5
        var tarjeta = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 3m, [(MetodoPago.Tarjeta, 30m)], sesionCajero.Id); // no cash
        var legado = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 1m, [(MetodoPago.Efectivo, 10m)], sesionCajero.Id);
        var mismaCaja = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 1m, [(MetodoPago.Efectivo, 10m)], sesionCajero.Id);
        var sinSesion = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 1m, [(MetodoPago.Efectivo, 10m)]); // net cash 10
        _ = await _seeder.SeedVentaCobradaAsync(sucursalId, adminId, loteId, 4m, [(MetodoPago.Efectivo, 40m)], sesionAdmin.Id); // net cash 40
        _ = DbContext.Ventas.Add(Venta.Create(sucursalId, cajeroId, null, sesionCajero.Id, [DetalleVenta.Create(loteId, 9m, 10m, 8m).Value!], EstadoVenta.Pendiente).Value!);
        _ = efectivo.Anular("Error", sesionAdmin.Id);
        _ = mixta.Anular("Error", sesionAdmin.Id);
        _ = tarjeta.Anular("Error", sesionAdmin.Id);
        _ = legado.Anular("Error");
        _ = mismaCaja.Anular("Error", sesionCajero.Id);
        _ = sinSesion.Anular("Error", sesionAdmin.Id);
        _ = await DbContext.SaveChangesAsync();
        DbContext.ChangeTracker.Clear();

        var esperadoCajero = await EsperadoAsync(sesionCajero);
        var esperadoAdmin = await EsperadoAsync(sesionAdmin);

        _ = esperadoCajero.Should().Be(115m, because: "100 + 10 + 5 of the sales it collected, whose cash was handed back from another drawer");
        _ = esperadoAdmin.Should().Be(215m, because: "200 + 40 collected - 10 - 5 - 10 handed back for sales of other drawers");
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectAVoidSession_WhenItDoesNotExist()
    {
        var (sucursalId, cajeroId, _, loteId) = await SeedSucursalAsync();
        var venta = await _seeder.SeedVentaCobradaAsync(sucursalId, cajeroId, loteId, 1m, [(MetodoPago.Efectivo, 10m)]);
        _ = venta.Anular("Error", Guid.NewGuid());

        var act = () => DbContext.SaveChangesAsync();

        _ = await act.Should().ThrowAsync<DbUpdateException>(because: "Sesion_Anulacion_ID references POS_Sesiones_Caja");
    }
}
