using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Devoluciones.Queries.GetDevoluciones;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Devoluciones.Common;

public static class DevolucionResumenExtensions
{
    private const string ClienteGeneral = "Público General";
    private const string SinDato = "N/A";

    // Related display data is loaded per page and joined in memory: projecting a paginated query over required navigations skews COUNT against the rows
    public static async Task<List<DevolucionResumenDto>> GetResumenesAsync(this IApplicationDbContext context, IReadOnlyCollection<DevolucionCabecera> devoluciones, CancellationToken cancellationToken)
    {
        var origenIds = devoluciones.Select(d => d.ComprobanteOrigenId).Distinct().ToList();
        var origenes = await context.SUNATComprobantesEmitidos
            .AsNoTracking()
            .Include(c => c.Serie)
            .Where(c => origenIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var ncIds = devoluciones.Where(d => d.ComprobanteNcId.HasValue).Select(d => d.ComprobanteNcId!.Value).Distinct().ToList();
        var notasCredito = await context.SUNATComprobantesEmitidos
            .AsNoTracking()
            .Include(c => c.Serie)
            .Where(c => ncIds.Contains(c.Id))
            .ToDictionaryAsync(c => c.Id, cancellationToken);

        var ventaIds = origenes.Values.Select(c => c.TransaccionId).Distinct().ToList();
        var ventas = await context.Ventas
            .AsNoTracking()
            .Where(v => ventaIds.Contains(v.Id))
            .Select(v => new
            {
                v.Id,
                SucursalNombre = v.Sucursal != null ? v.Sucursal.Nombre : null,
                ClienteNombre = v.Cliente != null ? v.Cliente.NombreApellidos : null,
            })
            .ToDictionaryAsync(v => v.Id, cancellationToken);

        var empleadoIds = devoluciones.Select(d => d.EmpleadoAutorizaId).Distinct().ToList();
        var empleados = await context.Empleados
            .AsNoTracking()
            .Where(e => empleadoIds.Contains(e.Id))
            .Select(e => new { e.Id, Nombre = e.Nombres + " " + e.Apellido_Paterno })
            .ToDictionaryAsync(e => e.Id, e => e.Nombre, cancellationToken);

        return [.. devoluciones.Select(d =>
        {
            var origen = origenes.GetValueOrDefault(d.ComprobanteOrigenId);
            var venta = origen is null ? null : ventas.GetValueOrDefault(origen.TransaccionId);
            var notaCredito = d.ComprobanteNcId.HasValue ? notasCredito.GetValueOrDefault(d.ComprobanteNcId.Value) : null;

            return new DevolucionResumenDto(
                d.Id,
                d.ComprobanteOrigenId,
                d.EmpleadoAutorizaId,
                d.FechaDevolucion,
                d.MotivoSunatCatalogo,
                d.SustentoDescriptivo,
                origen?.TransaccionId,
                origen is null ? null : Numero(origen),
                notaCredito is null ? null : Numero(notaCredito),
                venta?.ClienteNombre ?? ClienteGeneral,
                empleados.GetValueOrDefault(d.EmpleadoAutorizaId) ?? SinDato,
                venta?.SucursalNombre,
                notaCredito?.MontoTotalVenta,
                d.MontoReembolsado,
                d.MetodoReembolso?.ToString());
        })];
    }

    // Same format the sale and its receipts print: series prefix, a dash and the correlative with 8 digits
    private static string Numero(SunatComprobanteEmitido comprobante) => $"{comprobante.Serie?.PrefijoSerie}-{comprobante.NumeroCorrelativo:D8}";
}
