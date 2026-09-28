using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.SeriesFiscales.Queries.GetSeriesFiscales;

public record GetSeriesFiscalesQuery : IRequest<Result<PaginatedList<SerieFiscalDto>>>
{
    public Guid? SucursalId { get; init; }
    public TipoComprobante? TipoComprobante { get; init; }
    public string? EstadoSerie { get; init; }
    public string? PrefijoSerie { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
