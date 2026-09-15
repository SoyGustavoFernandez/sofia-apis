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
    public Guid? ClienteId { get; init; }
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
