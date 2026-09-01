using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Queries.GetUnidadesMedida;

public record GetUnidadesMedidaQuery : IRequest<Result<PaginatedList<UnidadMedidaDto>>>
{
    public string? Codigo { get; init; }
    public string? Descripcion { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
