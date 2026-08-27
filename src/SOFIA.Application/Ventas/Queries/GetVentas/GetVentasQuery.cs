using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Ventas.Queries.GetVentas;

public record GetVentasQuery : IRequest<Result<PaginatedList<VentaDto>>>
{
    public DateTime? FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public EstadoVenta? Estado { get; init; }
    public Guid? EmpleadoId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public record VentaDto(
    Guid Id,
    string CodigoVenta,
    DateTime FechaHora,
    decimal Total,
    string Estado,
    string EmpleadoNombre,
    string? ClienteNombre,
    int ItemsCount);

public class GetVentasQueryHandler(
    IApplicationDbContext context,
    ICurrentUser currentUser) : IRequestHandler<GetVentasQuery, Result<PaginatedList<VentaDto>>>
{
    public async Task<Result<PaginatedList<VentaDto>>> Handle(GetVentasQuery request, CancellationToken cancellationToken)
    {
        if (!currentUser.IsAuthenticated || string.IsNullOrEmpty(currentUser.SucursalId))
        {
            return Result.Failure<PaginatedList<VentaDto>>(Error.Unauthorized("Venta.Auth", "Usuario no autenticado."));
        }

        if (!Guid.TryParse(currentUser.SucursalId, out var sucursalId))
        {
            return Result.Failure<PaginatedList<VentaDto>>(Error.Validation("Venta.Sucursal", "ID de sucursal inválido."));
        }

        var query = context.Ventas
            .AsNoTracking()
            .Include(v => v.Empleado)
            .Include(v => v.Detalles)
            .Where(v => v.SucursalId == sucursalId);

        // Apply filters
        if (request.FechaInicio.HasValue)
        {
            query = query.Where(v => v.FechaHoraUtc >= request.FechaInicio.Value);
        }

        if (request.FechaFin.HasValue)
        {
            query = query.Where(v => v.FechaHoraUtc <= request.FechaFin.Value);
        }

        if (request.Estado.HasValue)
        {
            query = query.Where(v => v.Estado == request.Estado.Value);
        }

        if (request.EmpleadoId.HasValue)
        {
            query = query.Where(v => v.EmpleadoId == request.EmpleadoId.Value);
        }

        // 1. Obtener la lista paginada de entidades (el motor de EF hace el trabajo pesado)
        var paginatedEntities = await PaginatedList<Venta>.CreateAsync(
            query.OrderByDescending(v => v.FechaHoraUtc),
            request.PageNumber,
            request.PageSize);

        // 2. Mapear a DTO en memoria (código limpio y legible)
        var dtos = paginatedEntities.Items.Select(v => new VentaDto(
            v.Id,
            v.Id.ToString()[..8].ToUpper(),
            v.FechaHoraUtc,
            v.MontoTotalBruto,
            v.Estado.ToString(),
            v.Empleado != null ? $"{v.Empleado.Nombres} {v.Empleado.Apellido_Paterno}" : "N/A",
            v.ClienteId?.ToString()[..8] ?? "Público General",
            v.Detalles.Count)).ToList();

        // 3. Devolver la nueva lista paginada
        var result = new PaginatedList<VentaDto>(
            dtos,
            paginatedEntities.TotalCount,
            paginatedEntities.PageNumber,
            request.PageSize);

        return Result.Success(result);
    }
}
