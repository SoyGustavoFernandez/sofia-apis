using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Queries.GetUnidadesMedida;

public record GetUnidadesMedidaQuery : IRequest<Result<PaginatedList<UnidadMedidaDto>>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetUnidadesMedidaQueryHandler(IApplicationDbContext context) : IRequestHandler<GetUnidadesMedidaQuery, Result<PaginatedList<UnidadMedidaDto>>>
{
    public async Task<Result<PaginatedList<UnidadMedidaDto>>> Handle(GetUnidadesMedidaQuery request, CancellationToken cancellationToken)
    {
        var query = context.UnidadesMedida
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.Codigo.ToLower().Contains(searchTerm) ||
                                     x.Descripcion.ToLower().Contains(searchTerm));
        }

        var paginatedList = await PaginatedList<UnidadMedidaDto>.CreateAsync(
            query.Select(x => new UnidadMedidaDto(x.Id, x.Codigo, x.Descripcion)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
