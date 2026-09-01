using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empresas.Queries.GetEmpresas;

public record GetEmpresasQuery : IRequest<Result<PaginatedList<EmpresaDto>>>
{
    public string? Nombre { get; init; }
    public EstadoEmpresa? Estado { get; init; }
    public DateTimeOffset? FechaVencimientoDesde { get; init; }
    public DateTimeOffset? FechaVencimientoHasta { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 25;
}
