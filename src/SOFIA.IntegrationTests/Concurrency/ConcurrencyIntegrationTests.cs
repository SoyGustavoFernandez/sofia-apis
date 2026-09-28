using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Persistence;
using SOFIA.IntegrationTests.Infrastructure;

namespace SOFIA.IntegrationTests.Concurrency;

public class ConcurrencyIntegrationTests(SofiaWebAppFactory factory) : BaseIntegrationTest(factory)
{
    [Fact]
    public async Task SaveChanges_ShouldRejectStockWrite_WhenAnotherRequestChangedTheRowSinceItWasRead()
    {
        // Arrange: two requests read the same 5 units
        var inventarioId = await SeedInventarioAsync(5m);

        using var scopeA = Factory.CreateTestScope();
        using var scopeB = Factory.CreateTestScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var inventarioA = await contextA.LotesEnSucursal.SingleAsync(i => i.Id == inventarioId);
        var inventarioB = await contextB.LotesEnSucursal.SingleAsync(i => i.Id == inventarioId);

        // Act: both sell the last 5 units from their own read
        inventarioA.UpdateStock(inventarioA.CantidadFisica - 5m);
        _ = await contextA.SaveChangesAsync();

        inventarioB.UpdateStock(inventarioB.CantidadFisica - 5m);
        var act = () => contextB.SaveChangesAsync();

        // Assert
        _ = await act.Should().ThrowAsync<DbUpdateConcurrencyException>(because: "the second sale was computed from a stale quantity");
        var persisted = await DbContext.LotesEnSucursal.AsNoTracking().SingleAsync(i => i.Id == inventarioId);
        _ = persisted.CantidadFisica.Should().Be(0m);
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectSecondStateTransition_WhenTheSaleWasChangedSinceItWasRead()
    {
        // Arrange
        var (sucursalId, empleadoId) = await SeedSucursalEmpleadoAsync();
        var inventarioId = await SeedInventarioAsync(5m, sucursalId);
        var loteId = (await DbContext.LotesEnSucursal.AsNoTracking().SingleAsync(i => i.Id == inventarioId)).LoteId;
        var venta = Venta.Create(sucursalId, empleadoId, null, null, [DetalleVenta.Create(loteId, 1m, 10m, 8m).Value!]).Value!;
        _ = DbContext.Ventas.Add(venta);
        _ = await DbContext.SaveChangesAsync();

        using var scopeA = Factory.CreateTestScope();
        using var scopeB = Factory.CreateTestScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ventaA = await contextA.Ventas.SingleAsync(v => v.Id == venta.Id);
        var ventaB = await contextB.Ventas.SingleAsync(v => v.Id == venta.Id);

        // Act: both see Completada and void it
        _ = ventaA.Anular("Error de cobro");
        _ = await contextA.SaveChangesAsync();

        _ = ventaB.Anular("Duplicado");
        var act = () => contextB.SaveChangesAsync();

        // Assert
        _ = await act.Should().ThrowAsync<DbUpdateConcurrencyException>();
        var persisted = await DbContext.Ventas.AsNoTracking().SingleAsync(v => v.Id == venta.Id);
        _ = persisted.Estado.Should().Be(EstadoVenta.Anulada);
        _ = persisted.MotivoAnulacion.Should().Be("Error de cobro");
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectSecondPartialReturn_WhenBothWereCappedAgainstTheSameRead()
    {
        // Arrange: 5 units sold, two clerks each return 3 based on the same "nothing returned yet" read
        var (sucursalId, empleadoId) = await SeedSucursalEmpleadoAsync();
        var inventarioId = await SeedInventarioAsync(5m, sucursalId);
        var loteId = (await DbContext.LotesEnSucursal.AsNoTracking().SingleAsync(i => i.Id == inventarioId)).LoteId;
        var venta = Venta.Create(sucursalId, empleadoId, null, null, [DetalleVenta.Create(loteId, 5m, 10m, 8m).Value!]).Value!;
        _ = DbContext.Ventas.Add(venta);
        _ = await DbContext.SaveChangesAsync();

        using var scopeA = Factory.CreateTestScope();
        using var scopeB = Factory.CreateTestScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var contextB = scopeB.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var ventaA = await contextA.Ventas.Include(v => v.Detalles).SingleAsync(v => v.Id == venta.Id);
        var ventaB = await contextB.Ventas.Include(v => v.Detalles).SingleAsync(v => v.Id == venta.Id);
        var detalleId = ventaA.Detalles.Single().Id;

        // Act
        _ = ventaA.RegistrarDevolucion(new Dictionary<Guid, decimal> { [detalleId] = 3m }, new Dictionary<Guid, decimal>());
        _ = await contextA.SaveChangesAsync();

        _ = ventaB.RegistrarDevolucion(new Dictionary<Guid, decimal> { [detalleId] = 3m }, new Dictionary<Guid, decimal>());
        var act = () => contextB.SaveChangesAsync();

        // Assert
        _ = await act.Should().ThrowAsync<DbUpdateConcurrencyException>(because: "a partial return still writes the sale row, so the second cap check is stale");
    }

    [Fact]
    public async Task SaveChanges_ShouldRejectSecondOpenCashSession_WhenTheCashierAlreadyHasOne()
    {
        // Arrange
        var (sucursalId, empleadoId) = await SeedSucursalEmpleadoAsync();
        _ = DbContext.POSSesionesCaja.Add(PosSesionCaja.Create(sucursalId, empleadoId, DateTime.UtcNow, 100m).Value!);
        _ = await DbContext.SaveChangesAsync();

        // Act
        _ = DbContext.POSSesionesCaja.Add(PosSesionCaja.Create(sucursalId, empleadoId, DateTime.UtcNow, 50m).Value!);
        var act = () => DbContext.SaveChangesAsync();

        // Assert
        var thrown = await act.Should().ThrowAsync<DbUpdateException>();
        _ = thrown.Which.InnerException!.Message.Should().Contain("UX_POS_Sesiones_Caja_Empleado_Abierta");
    }

    private async Task<(Guid SucursalId, Guid EmpleadoId)> SeedSucursalEmpleadoAsync()
    {
        var sucursal = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. Test 123", $"LIC{Guid.NewGuid():N}"[..10]).Value!;
        _ = DbContext.Sucursales.Add(sucursal);
        _ = await DbContext.SaveChangesAsync();

        var empleado = Empleado.Create(sucursal.Id, "Ana", "Perez", "Gomez").Value!;
        _ = DbContext.Empleados.Add(empleado);
        _ = await DbContext.SaveChangesAsync();

        return (sucursal.Id, empleado.Id);
    }

    private async Task<Guid> SeedInventarioAsync(decimal cantidad, Guid? sucursalId = null)
    {
        var laboratorio = Laboratorio.Create($"Lab{Guid.NewGuid():N}"[..20], null).Value!;
        var unidad = UnidadMedida.Create($"U{Guid.NewGuid():N}"[..10], "Unidad Test").Value!;
        _ = DbContext.Laboratorios.Add(laboratorio);
        _ = DbContext.UnidadesMedida.Add(unidad);
        _ = await DbContext.SaveChangesAsync();

        var medicamento = Medicamento.Create($"MED-{Guid.NewGuid():N}"[..20], "Paracetamol Test", laboratorio.Id, unidad.Id, CondicionVenta.VentaLibreOTC, 10m).Value!;
        _ = DbContext.Medicamentos.Add(medicamento);
        _ = await DbContext.SaveChangesAsync();

        var lote = LoteInventario.Create(medicamento.Id, $"L{Guid.NewGuid():N}"[..12], null, DateTimeOffset.UtcNow.AddYears(1)).Value!;
        _ = DbContext.LotesInventario.Add(lote);

        if (sucursalId is null)
        {
            var sucursal = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. Test 123", $"LIC{Guid.NewGuid():N}"[..10]).Value!;
            _ = DbContext.Sucursales.Add(sucursal);
            sucursalId = sucursal.Id;
        }

        _ = await DbContext.SaveChangesAsync();

        var inventario = InventarioSucursal.Create(sucursalId.Value, lote.Id, cantidad).Value!;
        _ = DbContext.LotesEnSucursal.Add(inventario);
        _ = await DbContext.SaveChangesAsync();

        return inventario.Id;
    }
}
