using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Extensions;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Queries.GetVentas;

public class GetVentasQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetVentasQuery, Result<PaginatedList<VentaDto>>>
{
    public async Task<Result<PaginatedList<VentaDto>>> Handle(GetVentasQuery request, CancellationToken cancellationToken)
    {
        var sucursalResult = currentUser.GetSucursalId();
        if (sucursalResult.IsFailure)
        {
            return Result.Failure<PaginatedList<VentaDto>>(sucursalResult.Error);
        }

        var sucursalId = sucursalResult.Value;

        // No .Include() here on purpose: EF Core's CountAsync() throws with combined reference+collection Includes; reference navs are fetched separately below.
        var query = context.Ventas
            .AsNoTracking()
            .Where(v => v.SucursalId == sucursalId);

        // Apply filters
        query = query.WhereDateRange(v => v.FechaHoraUtc, request.FechaInicio, request.FechaFin);

        if (request.Estado.HasValue)
        {
            query = query.Where(v => v.Estado == request.Estado.Value);
        }

        if (request.EmpleadoId.HasValue)
        {
            query = query.Where(v => v.EmpleadoId == request.EmpleadoId.Value);
        }

        if (request.ClienteId.HasValue)
        {
            query = query.Where(v => v.ClienteId == request.ClienteId.Value);
        }

        // 1. Fetch the paginated entity list (EF does the heavy lifting)
        var paginatedEntities = await PaginatedList<Venta>.CreateAsync(
            query.OrderByDescending(v => v.FechaHoraUtc),
            request.PageNumber,
            request.PageSize);

        var ventaIds = paginatedEntities.Items.Select(v => v.Id).ToList();

        // 2. Fetch comprobantes for the page's ventas separately (in-memory join, avoids the paginated-projection bug).
        var comprobantes = await context.SUNATComprobantesEmitidos
            .AsNoTracking()
            .Include(c => c.Serie)
            .Where(c => ventaIds.Contains(c.TransaccionId) && !c.IsDeleted)
            .ToDictionaryAsync(c => c.TransaccionId, cancellationToken);

        // 3. Fetch related display fields via a Select projection (no Include, safe with the page's id list).
        var extras = await context.Ventas
            .AsNoTracking()
            .Where(v => ventaIds.Contains(v.Id))
            .Select(v => new
            {
                v.Id,
                EmpleadoNombre = v.Empleado != null ? v.Empleado.Nombres + " " + v.Empleado.Apellido_Paterno : null,
                ClienteNombre = v.Cliente != null ? v.Cliente.NombreApellidos : null,
                ItemsCount = v.Detalles.Count,
            })
            .ToDictionaryAsync(x => x.Id, cancellationToken);

        // 4. Map to DTO in memory
        var dtos = paginatedEntities.Items.Select(v => new VentaDto(
            v.Id,
            comprobantes.TryGetValue(v.Id, out var comprobante)
                ? $"{comprobante.Serie?.PrefijoSerie}-{comprobante.NumeroCorrelativo:D8}"
                : v.Id.ToString()[..8].ToUpper(),
            v.FechaHoraUtc,
            v.MontoTotalBruto,
            v.Estado.ToString(),
            extras.TryGetValue(v.Id, out var extra) ? extra.EmpleadoNombre ?? "N/A" : "N/A",
            extras.TryGetValue(v.Id, out extra) ? extra.ClienteNombre ?? "Público General" : "Público General",
            extras.TryGetValue(v.Id, out extra) ? extra.ItemsCount : 0)).ToList();

        // 5. Return the new paginated list
        var result = new PaginatedList<VentaDto>(
            dtos,
            paginatedEntities.TotalCount,
            paginatedEntities.PageNumber,
            request.PageSize);

        return Result.Success(result);
    }
}
