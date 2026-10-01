using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;

namespace SOFIA.Application.Devoluciones.Common;

public static class CantidadesDevueltasExtensions
{
    // Sums what live returns already took back per sale line, so the next return and the sale detail agree on what remains
    public static async Task<Dictionary<Guid, decimal>> GetCantidadesDevueltasAsync(this IApplicationDbContext dbContext, IReadOnlyCollection<Guid> detalleVentaIds, CancellationToken cancellationToken)
    {
        var devueltas = await (
            from dd in dbContext.DetallesDevolucion
            join dc in dbContext.Devoluciones on dd.DevolucionId equals dc.Id
            where detalleVentaIds.Contains(dd.DetalleVentaId) && !dd.IsDeleted && !dc.IsDeleted
            select new { dd.DetalleVentaId, dd.CantidadDevuelta }).ToListAsync(cancellationToken);

        return devueltas
            .GroupBy(d => d.DetalleVentaId)
            .ToDictionary(g => g.Key, g => g.Sum(d => d.CantidadDevuelta));
    }
}
