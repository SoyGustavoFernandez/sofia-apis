using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Proveedores.Queries.GetPreciosProveedor;

public class GetPreciosProveedorQueryHandler(IApplicationDbContext context) : IRequestHandler<GetPreciosProveedorQuery, Result<PaginatedList<PrecioProveedorDto>>>
{
    public async Task<Result<PaginatedList<PrecioProveedorDto>>> Handle(GetPreciosProveedorQuery request, CancellationToken cancellationToken)
    {
        var proveedorExists = await context.Proveedores.AnyAsync(x => x.Id == request.ProveedorId, cancellationToken);
        if (!proveedorExists)
        {
            return Result.Failure<PaginatedList<PrecioProveedorDto>>(Error.NotFound("Proveedor.NotFound", $"Proveedor with ID {request.ProveedorId} was not found."), 404);
        }

        // The page is paginated on the entity and the product names are fetched separately, so the COUNT never depends on a navigation
        var page = await PaginatedList<HistorialPrecioProveedor>.CreateAsync(
            context.HistorialPreciosProveedor
                .AsNoTracking()
                .Where(h => h.ProveedorId == request.ProveedorId)
                .OrderByDescending(h => h.FechaInicioVigencia)
                .ThenByDescending(h => h.CreatedAt)
                .ThenBy(h => h.Id),
            request.PageNumber,
            request.PageSize);

        var productoIds = page.Items.Select(h => h.ProductoId).Distinct().ToList();
        var nombres = await context.Medicamentos
            .AsNoTracking()
            .Where(m => productoIds.Contains(m.Id))
            .ToDictionaryAsync(m => m.Id, m => m.NombreComercial, cancellationToken);

        var ahora = DateTime.UtcNow;
        var dtos = page.Items
            .Select(h => new PrecioProveedorDto(
                h.Id,
                h.ProductoId,
                nombres.GetValueOrDefault(h.ProductoId, string.Empty),
                h.CostoPorUnidadBase,
                h.FechaInicioVigencia,
                h.FechaFinVigencia,
                h.FechaInicioVigencia <= ahora && (h.FechaFinVigencia is null || h.FechaFinVigencia >= ahora)))
            .ToList();

        return Result.Success(new PaginatedList<PrecioProveedorDto>(dtos, page.TotalCount, page.PageNumber, request.PageSize));
    }
}
