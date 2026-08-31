using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiasUoM;

public record GetJerarquiasUoMQuery : IRequest<Result<PaginatedList<JerarquiaUoMDto>>>
{
    public Guid? ProductoId { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}
