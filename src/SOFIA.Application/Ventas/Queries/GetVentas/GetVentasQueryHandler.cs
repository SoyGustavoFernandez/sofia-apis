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

        var query = context.Ventas
            .AsNoTracking()
            .Include(v => v.Empleado)
            .Include(v => v.Cliente)
            .Include(v => v.Detalles)
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

        // 1. Fetch the paginated entity list (EF does the heavy lifting)
        var paginatedEntities = await PaginatedList<Venta>.CreateAsync(
            query.OrderByDescending(v => v.FechaHoraUtc),
            request.PageNumber,
            request.PageSize);

        // 2. Map to DTO in memory
        var dtos = paginatedEntities.Items.Select(v => new VentaDto(
            v.Id,
            v.Id.ToString()[..8].ToUpper(),
            v.FechaHoraUtc,
            v.MontoTotalBruto,
            v.Estado.ToString(),
            v.Empleado != null ? $"{v.Empleado.Nombres} {v.Empleado.Apellido_Paterno}" : "N/A",
            v.Cliente?.NombreApellidos ?? "Público General",
            v.Detalles.Count)).ToList();

        // 3. Return the new paginated list
        var result = new PaginatedList<VentaDto>(
            dtos,
            paginatedEntities.TotalCount,
            paginatedEntities.PageNumber,
            request.PageSize);

        return Result.Success(result);
    }
}
