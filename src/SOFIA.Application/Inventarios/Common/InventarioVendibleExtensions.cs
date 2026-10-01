using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Inventarios.Common;

public static class InventarioVendibleExtensions
{
    // Mirrors the sale rules in VentaDetalleFactory: own branch, in stock, not expired and not held in quarantine.
    public static IQueryable<InventarioSucursal> WhereVendibleEnSucursal(
        this IQueryable<InventarioSucursal> source,
        IQueryable<DigemidInventarioCuarentena> cuarentenas,
        Guid sucursalId,
        DateTimeOffset ahora) => source
            .Where(x => x.SucursalId == sucursalId && x.CantidadFisica > 0)
            .WhereLoteNoVencido(ahora)
            .Where(x => !cuarentenas.Any(q => q.LoteId == x.LoteId && q.EstadoResolucion == EstadoResolucionCuarentena.Retenido));

    // SQL form of LoteInventario.EstaVencido: the threshold is computed once so both sides share the domain rule.
    public static IQueryable<InventarioSucursal> WhereLoteNoVencido(this IQueryable<InventarioSucursal> source, DateTimeOffset ahora)
    {
        var ultimaCaducidadVencida = LoteInventario.UltimaCaducidadVencida(ahora);

        return source.Where(x => x.Lote != null && x.Lote.FechaCaducidad > ultimaCaducidadVencida);
    }
}
