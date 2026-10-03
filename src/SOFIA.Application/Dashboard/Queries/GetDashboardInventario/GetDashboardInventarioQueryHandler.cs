using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Dashboard.Common;
using SOFIA.Application.Inventarios.Common;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardInventario;

public class GetDashboardInventarioQueryHandler(IApplicationDbContext context, ISucursalAccess sucursalAccess)
    : IRequestHandler<GetDashboardInventarioQuery, Result<DashboardInventarioDto>>
{
    // Sellable base units at or below which a product counts as low on stock in a branch; same value the POS warns at
    public const decimal UmbralStockBajo = 10m;

    public async Task<Result<DashboardInventarioDto>> Handle(GetDashboardInventarioQuery request, CancellationToken cancellationToken)
    {
        var scope = await DashboardScopeResolver.ResolveAsync(context, sucursalAccess, cancellationToken);
        var ids = scope.Ids;

        // Same rows the stock-by-branch screen lists, so every lot figure matches that screen
        var filas = context.LotesEnSucursal
            .AsNoTracking()
            .Where(x => x.Sucursal != null && x.Lote != null && x.Lote.Producto != null)
            .Where(x => ids.Contains(x.SucursalId));

        // A lot sells through the end of its expiry day, so one expiring today is not expired yet
        var hoy = request.Hoy;
        var limite30 = hoy.AddDays(31);
        var limite60 = hoy.AddDays(61);
        var limite90 = hoy.AddDays(91);
        var conStock = filas.Where(x => x.CantidadFisica > 0);

        var vencidos = await conStock.CountAsync(x => x.Lote!.FechaCaducidad < hoy, cancellationToken);
        var vencen30 = await conStock.CountAsync(x => x.Lote!.FechaCaducidad >= hoy && x.Lote.FechaCaducidad < limite30, cancellationToken);
        var vencen60 = await conStock.CountAsync(x => x.Lote!.FechaCaducidad >= limite30 && x.Lote.FechaCaducidad < limite60, cancellationToken);
        var vencen90 = await conStock.CountAsync(x => x.Lote!.FechaCaducidad >= limite60 && x.Lote.FechaCaducidad < limite90, cancellationToken);

        // Stock alerts are per branch and product; rows are filtered to sellable before grouping because SQL Server rejects a subquery inside an aggregate
        var productosEnSucursal = await filas
            .Select(x => new { x.SucursalId, x.Lote!.ProductoId })
            .Distinct()
            .CountAsync(cancellationToken);

        var vendiblePorProducto = filas
            .WhereVendible(context.DigemidInventarioCuarentena, DateTimeOffset.UtcNow)
            .GroupBy(x => new { x.SucursalId, x.Lote!.ProductoId })
            .Select(g => g.Sum(x => x.CantidadFisica));

        var conVendible = await vendiblePorProducto.CountAsync(cancellationToken);
        var stockBajo = await vendiblePorProducto.CountAsync(total => total <= UmbralStockBajo, cancellationToken);

        return Result.Success(new DashboardInventarioDto(
            vencidos,
            vencen30,
            vencen60,
            vencen90,
            productosEnSucursal - conVendible,
            stockBajo,
            UmbralStockBajo));
    }
}
