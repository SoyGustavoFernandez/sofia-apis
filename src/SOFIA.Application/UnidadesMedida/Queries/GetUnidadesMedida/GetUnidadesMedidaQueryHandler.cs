using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Queries.GetUnidadesMedida;

public class GetUnidadesMedidaQueryHandler(IApplicationDbContext context) : IRequestHandler<GetUnidadesMedidaQuery, Result<PaginatedList<UnidadMedidaDto>>>
{
    public async Task<Result<PaginatedList<UnidadMedidaDto>>> Handle(GetUnidadesMedidaQuery request, CancellationToken cancellationToken)
    {
        var query = context.UnidadesMedida
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.Codigo))
        {
            query = query.Where(x => x.Codigo.Contains(request.Codigo));
        }

        if (!string.IsNullOrWhiteSpace(request.Descripcion))
        {
            query = query.Where(x => x.Descripcion.Contains(request.Descripcion));
        }

        var paginatedList = await PaginatedList<UnidadMedidaDto>.CreateAsync(
            query.Select(x => new UnidadMedidaDto(x.Id, x.Codigo, x.Descripcion)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
