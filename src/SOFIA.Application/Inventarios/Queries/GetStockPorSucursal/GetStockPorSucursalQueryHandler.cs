using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetStockPorSucursal;

public class GetStockPorSucursalQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetStockPorSucursalQuery, Result<PaginatedList<StockPorSucursalDto>>>
{
    public async Task<Result<PaginatedList<StockPorSucursalDto>>> Handle(GetStockPorSucursalQuery request, CancellationToken cancellationToken)
    {
        // Sucursal, Lote and Producto are all required; a record missing any of them is invalid data and is excluded.
        var query = context.LotesEnSucursal
            .AsNoTracking()
            .Where(x => x.Sucursal != null && x.Lote != null && x.Lote.Producto != null);

        if (request.SoloConStock)
        {
            query = query.Where(x => x.CantidadFisica > 0);
        }

        if (!string.IsNullOrWhiteSpace(request.SucursalNombre))
        {
            var term = request.SucursalNombre.ToLower();
            query = query.Where(x => x.Sucursal!.Nombre.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.ProductoNombre))
        {
            var term = request.ProductoNombre.ToLower();
            query = query.Where(x => x.Lote!.Producto!.NombreComercial.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroLote))
        {
            var term = request.NumeroLote.ToLower();
            query = query.Where(x => x.Lote!.NumeroLoteMfr.ToLower().Contains(term));
        }

        if (request.CaducidadDesde.HasValue)
        {
            query = query.Where(x => x.Lote!.FechaCaducidad >= request.CaducidadDesde.Value);
        }

        if (request.CaducidadHasta.HasValue)
        {
            var hasta = request.CaducidadHasta.Value.AddDays(1);
            query = query.Where(x => x.Lote!.FechaCaducidad < hasta);
        }

        if (request.CantidadMin.HasValue)
        {
            query = query.Where(x => x.CantidadFisica >= request.CantidadMin.Value);
        }

        if (request.CantidadMax.HasValue)
        {
            query = query.Where(x => x.CantidadFisica <= request.CantidadMax.Value);
        }

        var paginatedList = await PaginatedList<StockPorSucursalDto>.CreateAsync(
            query.OrderBy(x => x.Sucursal!.Nombre)
                 .ThenBy(x => x.Lote!.Producto!.NombreComercial)
                 .ThenBy(x => x.Lote!.FechaCaducidad)
                 .Select(x => new StockPorSucursalDto(
                    x.Id,
                    x.SucursalId,
                    x.Sucursal!.Nombre,
                    x.LoteId,
                    x.Lote!.NumeroLoteMfr,
                    x.Lote.ProductoId,
                    x.Lote.Producto!.NombreComercial,
                    x.Lote.FechaCaducidad,
                    x.CantidadFisica,
                    x.Lote.Producto!.PrecioVentaBase)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
