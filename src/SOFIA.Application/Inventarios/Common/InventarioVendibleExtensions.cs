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
        DateTimeOffset ahora) => source.Where(x =>
                                          x.SucursalId == sucursalId
                                          && x.CantidadFisica > 0
                                          && x.Lote != null
                                          && x.Lote.FechaCaducidad > ahora
                                          && !cuarentenas.Any(q => q.LoteId == x.LoteId && q.EstadoResolucion == EstadoResolucionCuarentena.Retenido));
}
