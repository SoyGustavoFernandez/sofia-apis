using MediatR;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

public record GetDevolucionesQuery : IRequest<Result<PaginatedList<DevolucionResumenDto>>>
{
    public Guid? EmpleadoAutorizaId { get; init; }
    public DateTime? FechaInicio { get; init; }
    public DateTime? FechaFin { get; init; }
    public MetodoPago? MetodoReembolso { get; init; }
    public DestinoDevolucion? Destino { get; init; }
    public string? MotivoSunatCatalogo { get; init; }

    // Credit note number as printed ("BC01-00000007"), or just its series prefix or its correlative
    public string? NumeroNotaCredito { get; init; }

    public decimal? MontoMin { get; init; }
    public decimal? MontoMax { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
