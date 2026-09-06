using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetLotes;

public class GetLotesQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetLotesQuery, Result<PaginatedList<LoteInventarioDto>>>
{
    public async Task<Result<PaginatedList<LoteInventarioDto>>> Handle(GetLotesQuery request, CancellationToken cancellationToken)
    {
        // Producto is required; a lote whose medicamento was soft-deleted is invalid data and is excluded.
        var query = context.LotesInventario
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Producto != null);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.NumeroLoteMfr.ToLower().Contains(searchTerm) ||
                                     x.Producto!.NombreComercial.ToLower().Contains(searchTerm));
        }

        if (!string.IsNullOrWhiteSpace(request.ProductoNombre))
        {
            var term = request.ProductoNombre.ToLower();
            query = query.Where(x => x.Producto!.NombreComercial.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.NumeroLote))
        {
            var term = request.NumeroLote.ToLower();
            query = query.Where(x => x.NumeroLoteMfr.ToLower().Contains(term));
        }

        if (request.CaducidadDesde.HasValue)
        {
            query = query.Where(x => x.FechaCaducidad >= request.CaducidadDesde.Value);
        }

        if (request.CaducidadHasta.HasValue)
        {
            var hasta = request.CaducidadHasta.Value.AddDays(1);
            query = query.Where(x => x.FechaCaducidad < hasta);
        }

        if (request.FabricacionDesde.HasValue)
        {
            query = query.Where(x => x.FechaFabricacion != null && x.FechaFabricacion >= request.FabricacionDesde.Value);
        }

        if (request.FabricacionHasta.HasValue)
        {
            var hasta = request.FabricacionHasta.Value.AddDays(1);
            query = query.Where(x => x.FechaFabricacion != null && x.FechaFabricacion < hasta);
        }

        var paginatedList = await PaginatedList<LoteInventarioDto>.CreateAsync(
            query.OrderByDescending(x => x.CreatedAt)
                 .Select(x => new LoteInventarioDto(
                    x.Id,
                    x.ProductoId,
                    x.Producto!.NombreComercial,
                    x.NumeroLoteMfr,
                    x.FechaFabricacion,
                    x.FechaCaducidad)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
