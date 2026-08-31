using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.Enums;

namespace SOFIA.Application.Transferencias.Queries.GetTransferencias;

public record GetTransferenciasQuery : IRequest<Result<PaginatedList<TransferenciaDto>>>
{
    public Guid? SucursalFiltroId { get; init; }
    public EstadoLogistico? Estado { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
