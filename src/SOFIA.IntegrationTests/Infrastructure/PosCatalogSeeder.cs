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

    public async Task<Medicamento> SeedMedicamentoAsync(string nombre, CondicionVenta condicionVenta = CondicionVenta.VentaLibreOTC)
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

        var medicamento = Medicamento.Create($"MED-{Guid.NewGuid():N}"[..20], nombre, _laboratorioId, _unidadId, condicionVenta, 10m).Value!;
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

    public async Task<Guid> SeedEmpleadoAsync(Guid sucursalId)
    {
        var empleado = Empleado.Create(sucursalId, "Luis", "Rojas", "Diaz").Value!;
        _ = dbContext.Empleados.Add(empleado);
        _ = await dbContext.SaveChangesAsync();
        return empleado.Id;
    }

    public async Task<PosSesionCaja> SeedSesionAbiertaAsync(Guid sucursalId, Guid empleadoId, decimal montoApertura = 100m)
    {
        var sesion = PosSesionCaja.Create(sucursalId, empleadoId, DateTime.UtcNow, montoApertura).Value!;
        _ = dbContext.POSSesionesCaja.Add(sesion);
        _ = await dbContext.SaveChangesAsync();
        return sesion;
    }

    // A collected sale at unit price 10 with the payments the customer handed over, optionally into a cash session.
    public async Task<Venta> SeedVentaCobradaAsync(Guid sucursalId, Guid empleadoId, Guid loteId, decimal cantidad, (MetodoPago Metodo, decimal Monto)[] pagos, Guid? sesionId = null, DateTime? fechaUtc = null)
    {
        var venta = Venta.Create(sucursalId, empleadoId, null, sesionId, [DetalleVenta.Create(loteId, cantidad, 10m, 8m).Value!], EstadoVenta.Pendiente).Value!;
        var pagosVenta = pagos
            .Select(p => VentaPago.Create(p.Metodo, p.Monto, VentaPago.RequiereReferencia(p.Metodo) ? "OP-TEST" : null, DateTime.UtcNow).Value!)
            .ToList();
        if (venta.RegistrarPagos(pagosVenta).IsFailure)
        {
            throw new InvalidOperationException("The seeded payments do not cover the sale.");
        }

        _ = dbContext.Ventas.Add(venta);
        if (fechaUtc.HasValue)
        {
            dbContext.Entry(venta).Property(v => v.FechaHoraUtc).CurrentValue = fechaUtc.Value;
        }

        _ = await dbContext.SaveChangesAsync();
        return venta;
    }

    // Returns units of the sale's only line through its boleta, like the returns module does, refunding from the given session.
    public async Task<DevolucionCabecera> SeedDevolucionAsync(Venta venta, decimal cantidad, Guid sesionId, decimal montoReembolsado, MetodoPago metodoReembolso = MetodoPago.Efectivo, DateTime? fechaUtc = null)
    {
        var serie = SunatSerieFiscal.Create(venta.SucursalId, TipoComprobante.Boleta, $"B{Guid.NewGuid():N}"[..4].ToUpperInvariant(), 1, SunatSerieFiscal.EstadoInactiva).Value!;
        _ = dbContext.SUNATSeriesFiscales.Add(serie);
        var comprobante = SunatComprobanteEmitido.Create(
            venta.Id, serie.Id, 1, "1", "12345678", "CLIENTE TEST",
            venta.MontoTotalBruto, 0, 0, venta.MontoTotalBruto,
            null, EstadoComprobanteSunat.NoEnviado, null, null, null).Value!;
        _ = dbContext.SUNATComprobantesEmitidos.Add(comprobante);

        var detalleVenta = venta.Detalles.Single();
        if (venta.RegistrarDevolucion(new Dictionary<Guid, decimal> { [detalleVenta.Id] = cantidad }, new Dictionary<Guid, decimal>()).IsFailure)
        {
            throw new InvalidOperationException("The seeded sale cannot be returned.");
        }

        var devolucion = DevolucionCabecera.Create(
            comprobante.Id, null, venta.EmpleadoId, "07", "Devolucion de prueba", fechaUtc ?? DateTime.UtcNow,
            [DevolucionDetalle.Create(detalleVenta.Id, cantidad, DestinoDevolucion.Reingreso_Venta).Value!]).Value!;
        _ = devolucion.RegistrarReembolso(sesionId, montoReembolsado, metodoReembolso);
        _ = dbContext.Devoluciones.Add(devolucion);

        _ = await dbContext.SaveChangesAsync();
        return devolucion;
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
