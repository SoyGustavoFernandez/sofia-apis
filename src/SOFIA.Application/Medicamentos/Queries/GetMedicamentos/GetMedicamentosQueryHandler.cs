using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Application.Inventarios.Common;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Medicamentos.Queries.GetMedicamentos;

public class GetMedicamentosQueryHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<GetMedicamentosQuery, Result<PaginatedList<MedicamentoDto>>>
{
    private const int DiasVentanaMasVendidos = 30;

    public async Task<Result<PaginatedList<MedicamentoDto>>> Handle(GetMedicamentosQuery request, CancellationToken cancellationToken)
    {
        // Resolved only for the branch-scoped options, so the other screens keep working without a branch claim.
        var sucursalId = Guid.Empty;
        if (request.OrdenarPorMasVendidos || (request.IncluirStock && request.SoloStockVendibleEnMiSucursal))
        {
            var sucursalResult = currentUser.GetSucursalId();
            if (sucursalResult.IsFailure)
            {
                return Result.Failure<PaginatedList<MedicamentoDto>>(sucursalResult.Error);
            }

            sucursalId = sucursalResult.Value;
        }

        var query = Filtrar(request);
        var ordenados = request.OrdenarPorMasVendidos
            ? OrdenarPorMasVendidos(query, sucursalId)
            : query.OrderBy(x => x.NombreComercial)
                .Select(x => new MedicamentoProjection(
                    x.Id,
                    x.CodigoNacional,
                    x.NombreComercial,
                    x.LaboratorioId,
                    x.Laboratorio!.NombreCompania,
                    x.UnidadBaseId,
                    x.UnidadBase!.Descripcion,
                    x.CondicionVenta,
                    x.PrecioVentaBase));

        // Lightweight projection first — a stock join here would desync COUNT from the actual page.
        var pagina = await PaginatedList<MedicamentoProjection>.CreateAsync(ordenados, request.PageNumber, request.PageSize);

        // Skipped unless requested — the Excel export reuses this query at PageSize = PaginationLimits.MaxPageSize and never reads StockTotal.
        var stockPorProducto = request.IncluirStock
            ? await GetStockPorProductoAsync(request, [.. pagina.Items.Select(x => x.Id)], sucursalId, cancellationToken)
            : [];

        decimal? StockDe(Guid productoId)
        {
            return request.IncluirStock ? stockPorProducto.GetValueOrDefault(productoId) : null;
        }

        var dtos = pagina.Items.Select(x => new MedicamentoDto(
            x.Id,
            x.CodigoNacional,
            x.NombreComercial,
            x.LaboratorioId,
            x.LaboratorioNombre,
            x.UnidadBaseId,
            x.UnidadBaseNombre,
            x.CondicionVenta,
            x.PrecioVentaBase,
            StockDe(x.Id))).ToList();

        var paginatedList = new PaginatedList<MedicamentoDto>(
            dtos,
            pagina.TotalCount,
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }

    private IQueryable<Medicamento> Filtrar(GetMedicamentosQuery request)
    {
        // Laboratorio and UnidadBase are required; a medicamento whose principal was soft-deleted is invalid data and is excluded.
        var query = context.Medicamentos
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Laboratorio != null && x.UnidadBase != null);

        if (!string.IsNullOrWhiteSpace(request.CodigoNacional))
        {
            var term = request.CodigoNacional.ToLower();
            query = query.Where(x => x.CodigoNacional.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.NombreComercial))
        {
            var term = request.NombreComercial.ToLower();
            query = query.Where(x => x.NombreComercial.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.LaboratorioNombre))
        {
            var term = request.LaboratorioNombre.ToLower();
            query = query.Where(x => x.Laboratorio!.NombreCompania.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.UnidadBaseNombre))
        {
            var term = request.UnidadBaseNombre.ToLower();
            query = query.Where(x => x.UnidadBase!.Descripcion.ToLower().Contains(term));
        }

        if (request.CondicionVenta.HasValue)
        {
            query = query.Where(x => x.CondicionVenta == request.CondicionVenta.Value);
        }

        return query;
    }

    // Left join: the ranking only reorders the catalog, so unsold products still follow the best sellers.
    private IQueryable<MedicamentoProjection> OrdenarPorMasVendidos(IQueryable<Medicamento> query, Guid sucursalId)
    {
        var desde = DateTime.UtcNow.AddDays(-DiasVentanaMasVendidos);
        var vendidos = context.Ventas
            .AsNoTracking()
            .Where(v => v.SucursalId == sucursalId && v.FechaHoraUtc >= desde && v.Estado == EstadoVenta.Completada)
            .SelectMany(v => v.Detalles)
            .Where(d => d.Lote != null)
            .GroupBy(d => d.Lote!.ProductoId)
            .Select(g => new { ProductoId = g.Key, Total = g.Sum(d => d.CantidadVendida) });

        return
            from m in query
            join v in vendidos on m.Id equals v.ProductoId into ranking
            from v in ranking.DefaultIfEmpty()
            orderby (decimal?)v.Total ?? 0m descending, m.NombreComercial
            select new MedicamentoProjection(
                m.Id,
                m.CodigoNacional,
                m.NombreComercial,
                m.LaboratorioId,
                m.Laboratorio!.NombreCompania,
                m.UnidadBaseId,
                m.UnidadBase!.Descripcion,
                m.CondicionVenta,
                m.PrecioVentaBase);
    }

    private async Task<Dictionary<Guid, decimal>> GetStockPorProductoAsync(GetMedicamentosQuery request, List<Guid> medicamentoIds, Guid sucursalId, CancellationToken cancellationToken)
    {
        var stock = context.LotesEnSucursal
            .AsNoTracking()
            .Where(x => x.Lote != null && medicamentoIds.Contains(x.Lote.ProductoId));

        if (request.SoloStockVendibleEnMiSucursal)
        {
            stock = stock.WhereVendibleEnSucursal(context.DigemidInventarioCuarentena, sucursalId, DateTimeOffset.UtcNow);
        }

        return await stock
            .GroupBy(x => x.Lote!.ProductoId)
            .Select(g => new { ProductoId = g.Key, Total = g.Sum(x => x.CantidadFisica) })
            .ToDictionaryAsync(x => x.ProductoId, x => x.Total, cancellationToken);
    }

    private sealed record MedicamentoProjection(
        Guid Id,
        string CodigoNacional,
        string NombreComercial,
        Guid LaboratorioId,
        string LaboratorioNombre,
        Guid UnidadBaseId,
        string UnidadBaseNombre,
        CondicionVenta CondicionVenta,
        decimal? PrecioVentaBase);
}
