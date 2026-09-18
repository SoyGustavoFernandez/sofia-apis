using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Medicamentos.Queries.GetMedicamentos;

public class GetMedicamentosQueryHandler(IApplicationDbContext context, ICurrentUser currentUser) : IRequestHandler<GetMedicamentosQuery, Result<PaginatedList<MedicamentoDto>>>
{
    private const int DiasVentanaMasVendidos = 30;

    public async Task<Result<PaginatedList<MedicamentoDto>>> Handle(GetMedicamentosQuery request, CancellationToken cancellationToken)
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

        IReadOnlyCollection<MedicamentoProjection> items;
        int totalCount;

        if (request.OrdenarPorMasVendidos)
        {
            var sucursalResult = currentUser.GetSucursalId();
            if (sucursalResult.IsFailure)
            {
                return Result.Failure<PaginatedList<MedicamentoDto>>(sucursalResult.Error);
            }

            var desde = DateTime.UtcNow.AddDays(-DiasVentanaMasVendidos);
            var topProductoIds = await context.Ventas
                .AsNoTracking()
                .Where(v => v.SucursalId == sucursalResult.Value && v.FechaHoraUtc >= desde && v.Estado == EstadoVenta.Completada)
                .SelectMany(v => v.Detalles)
                .Where(d => d.Lote != null)
                .GroupBy(d => d.Lote!.ProductoId)
                .OrderByDescending(g => g.Sum(d => d.CantidadVendida))
                .Select(g => g.Key)
                .Take(request.PageSize)
                .ToListAsync(cancellationToken);

            var porId = await query
                .Where(x => topProductoIds.Contains(x.Id))
                .Select(x => new MedicamentoProjection(
                    x.Id,
                    x.CodigoNacional,
                    x.NombreComercial,
                    x.LaboratorioId,
                    x.Laboratorio!.NombreCompania,
                    x.UnidadBaseId,
                    x.UnidadBase!.Descripcion,
                    x.CondicionVenta,
                    x.PrecioVentaBase))
                .ToDictionaryAsync(x => x.Id, cancellationToken);

            // Preserve the sales-rank order — the query above no longer carries it once filtered by id.
            items = [.. topProductoIds.Where(porId.ContainsKey).Select(id => porId[id])];
            totalCount = items.Count;
        }
        else
        {
            // Lightweight projection first — a stock join here would desync COUNT from the actual page.
            var paginatedEntities = await PaginatedList<MedicamentoProjection>.CreateAsync(
                query.OrderBy(x => x.NombreComercial)
                    .Select(x => new MedicamentoProjection(
                        x.Id,
                        x.CodigoNacional,
                        x.NombreComercial,
                        x.LaboratorioId,
                        x.Laboratorio!.NombreCompania,
                        x.UnidadBaseId,
                        x.UnidadBase!.Descripcion,
                        x.CondicionVenta,
                        x.PrecioVentaBase)),
                request.PageNumber,
                request.PageSize);
            items = paginatedEntities.Items;
            totalCount = paginatedEntities.TotalCount;
        }

        // Skipped unless requested — the Excel export reuses this query at PageSize = int.MaxValue and never reads StockTotal.
        Dictionary<Guid, decimal> stockPorProducto = [];
        if (request.IncluirStock)
        {
            var medicamentoIds = items.Select(x => x.Id).ToList();
            stockPorProducto = await context.LotesEnSucursal
                .AsNoTracking()
                .Where(x => x.Lote != null && medicamentoIds.Contains(x.Lote.ProductoId))
                .GroupBy(x => x.Lote!.ProductoId)
                .Select(g => new { ProductoId = g.Key, Total = g.Sum(x => x.CantidadFisica) })
                .ToDictionaryAsync(x => x.ProductoId, x => x.Total, cancellationToken);
        }

        decimal? StockDe(Guid productoId)
        {
            return request.IncluirStock ? stockPorProducto.GetValueOrDefault(productoId) : null;
        }

        var dtos = items.Select(x => new MedicamentoDto(
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
            totalCount,
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
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
