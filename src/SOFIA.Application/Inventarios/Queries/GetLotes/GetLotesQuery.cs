using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Queries.GetLotes;

public record GetLotesQuery : IRequest<Result<PaginatedList<LoteInventarioDto>>>
{
    public string? SearchTerm { get; init; }
    public string? ProductoNombre { get; init; }
    public string? NumeroLote { get; init; }
    public DateTimeOffset? CaducidadDesde { get; init; }
    public DateTimeOffset? CaducidadHasta { get; init; }
    public DateTimeOffset? FabricacionDesde { get; init; }
    public DateTimeOffset? FabricacionHasta { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
