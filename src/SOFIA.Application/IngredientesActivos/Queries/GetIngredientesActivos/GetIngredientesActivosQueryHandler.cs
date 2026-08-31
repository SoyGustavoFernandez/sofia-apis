using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.IngredientesActivos.Queries.GetIngredientesActivos;

public class GetIngredientesActivosQueryHandler(IApplicationDbContext context) : IRequestHandler<GetIngredientesActivosQuery, Result<PaginatedList<IngredienteActivoDto>>>
{
    public async Task<Result<PaginatedList<IngredienteActivoDto>>> Handle(GetIngredientesActivosQuery request, CancellationToken cancellationToken)
    {
        var query = context.IngredientesActivos
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.DenominacionDci.ToLower().Contains(searchTerm) ||
                                     x.CodigoAtc.ToLower().Contains(searchTerm));
        }

        var paginatedList = await PaginatedList<IngredienteActivoDto>.CreateAsync(
            query.Select(x => new IngredienteActivoDto(x.Id, x.DenominacionDci, x.CodigoAtc)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
