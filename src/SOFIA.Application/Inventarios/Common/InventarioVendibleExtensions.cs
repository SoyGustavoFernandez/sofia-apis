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
            .Where(x => x.SucursalId == sucursalId)
            .WhereVendible(cuarentenas, ahora);

    // Branch-agnostic part of the sale rules, for callers that scope the branches themselves.
    public static IQueryable<InventarioSucursal> WhereVendible(
        this IQueryable<InventarioSucursal> source,
        IQueryable<DigemidInventarioCuarentena> cuarentenas,
        DateTimeOffset ahora) => source
            .Where(x => x.CantidadFisica > 0)
            .WhereLoteNoVencido(ahora)
            .Where(x => !cuarentenas.Any(q => q.LoteId == x.LoteId && q.EstadoResolucion == EstadoResolucionCuarentena.Retenido));

    // SQL form of LoteInventario.EstaVencido: the threshold is computed once so both sides share the domain rule.
    public static IQueryable<InventarioSucursal> WhereLoteNoVencido(this IQueryable<InventarioSucursal> source, DateTimeOffset ahora)
    {
        var ultimaCaducidadVencida = LoteInventario.UltimaCaducidadVencida(ahora);

        return source.Where(x => x.Lote != null && x.Lote.FechaCaducidad > ultimaCaducidadVencida);
    }
}
