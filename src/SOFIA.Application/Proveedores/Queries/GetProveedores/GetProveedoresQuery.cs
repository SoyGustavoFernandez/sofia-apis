using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Queries.GetProveedores;

public record ProveedorDto(Guid Id, string RazonSocial, string TaxId, string? TerminosFinancieros, decimal? CalificacionEsg, decimal TasaCumplimiento);

public record GetProveedoresQuery : IRequest<Result<PaginatedList<ProveedorDto>>>
{
    public string? RazonSocial { get; init; }
    public string? TaxId { get; init; }
    public decimal? TasaCumplimientoDesde { get; init; }
    public decimal? TasaCumplimientoHasta { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
