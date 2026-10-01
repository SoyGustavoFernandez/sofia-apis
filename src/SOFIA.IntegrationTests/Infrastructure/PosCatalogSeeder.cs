using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;
using SOFIA.Infrastructure.Persistence;

namespace SOFIA.IntegrationTests.Infrastructure;

/// <summary>Seeds the products, lots, stock and sales the POS catalog and lot lookups read.</summary>
public sealed class PosCatalogSeeder(ApplicationDbContext dbContext)
{
    private Guid _laboratorioId;
    private Guid _unidadId;

    public async Task<(Guid SucursalId, Guid EmpleadoId)> SeedSucursalAsync()
    {
        var sucursal = Sucursal.Create($"Suc{Guid.NewGuid():N}"[..20], "Av. Test 123", $"LIC{Guid.NewGuid():N}"[..10]).Value!;
        _ = dbContext.Sucursales.Add(sucursal);
        _ = await dbContext.SaveChangesAsync();

        var empleado = Empleado.Create(sucursal.Id, "Ana", "Perez", "Gomez").Value!;
        _ = dbContext.Empleados.Add(empleado);
        _ = await dbContext.SaveChangesAsync();

        return (sucursal.Id, empleado.Id);
    }

    public async Task<Medicamento> SeedMedicamentoAsync(string nombre)
    {
        if (_laboratorioId == Guid.Empty)
        {
            var laboratorio = Laboratorio.Create($"Lab{Guid.NewGuid():N}"[..20], null).Value!;
            var unidad = UnidadMedida.Create($"U{Guid.NewGuid():N}"[..10], "Unidad Test").Value!;
            _ = dbContext.Laboratorios.Add(laboratorio);
            _ = dbContext.UnidadesMedida.Add(unidad);
            _ = await dbContext.SaveChangesAsync();
            _laboratorioId = laboratorio.Id;
            _unidadId = unidad.Id;
        }

        var medicamento = Medicamento.Create($"MED-{Guid.NewGuid():N}"[..20], nombre, _laboratorioId, _unidadId, CondicionVenta.VentaLibreOTC, 10m).Value!;
        _ = dbContext.Medicamentos.Add(medicamento);
        _ = await dbContext.SaveChangesAsync();
        return medicamento;
    }

    // Expiry stored like the SPA sends it (-05:00) for an expiry day that started the given hours ago.
    public static DateTimeOffset CaducidadIniciadaHace(double horas) => DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(-5)).AddHours(-horas);

    // A lot can only be created with a future expiry, so an expired one is back-dated after creation.
    public async Task<LoteInventario> SeedLoteAsync(Guid productoId, Guid sucursalId, decimal cantidad, DateTimeOffset? caducidad = null)
    {
        var lote = LoteInventario.Create(productoId, $"L{Guid.NewGuid():N}"[..12], null, DateTimeOffset.UtcNow.AddYears(1)).Value!;
        _ = dbContext.LotesInventario.Add(lote);
        if (caducidad.HasValue)
        {
            dbContext.Entry(lote).Property(l => l.FechaCaducidad).CurrentValue = caducidad.Value;
        }

        _ = await dbContext.SaveChangesAsync();

        _ = dbContext.LotesEnSucursal.Add(InventarioSucursal.Create(sucursalId, lote.Id, cantidad).Value!);
        _ = await dbContext.SaveChangesAsync();
        return lote;
    }

    public async Task SeedVentaAsync(Guid sucursalId, Guid empleadoId, Guid loteId, decimal cantidad, EstadoVenta estado = EstadoVenta.Completada, DateTime? fechaUtc = null)
    {
        var venta = Venta.Create(sucursalId, empleadoId, null, null, [DetalleVenta.Create(loteId, cantidad, 10m, 8m).Value!], estado).Value!;
        _ = dbContext.Ventas.Add(venta);
        if (fechaUtc.HasValue)
        {
            dbContext.Entry(venta).Property(v => v.FechaHoraUtc).CurrentValue = fechaUtc.Value;
        }

        _ = await dbContext.SaveChangesAsync();
    }

    public async Task SeedCuarentenaAsync(Guid sucursalId, Guid loteId, Guid empleadoId, string estado)
    {
        _ = dbContext.DigemidInventarioCuarentena.Add(DigemidInventarioCuarentena.Create(sucursalId, loteId, null, 1m, "Alerta sanitaria", estado, empleadoId).Value!);
        _ = await dbContext.SaveChangesAsync();
    }

    public async Task<PresentacionVenta> SeedPresentacionAsync(Guid productoId, string descripcion, decimal unidadesBase, decimal precio)
    {
        var unidad = UnidadMedida.Create($"U{Guid.NewGuid():N}"[..10], descripcion).Value!;
        _ = dbContext.UnidadesMedida.Add(unidad);
        var presentacion = PresentacionVenta.Create(productoId, unidad.Id, descripcion, unidadesBase, precio).Value!;
        _ = dbContext.PresentacionesVenta.Add(presentacion);
        _ = await dbContext.SaveChangesAsync();
        return presentacion;
    }
}
