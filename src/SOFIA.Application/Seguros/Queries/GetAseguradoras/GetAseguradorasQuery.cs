using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Queries.GetAseguradoras;

public record AseguradoraDto(Guid Id, string NombreComercial, string CodigoIdentificadorNacional);

public record GetAseguradorasQuery : IRequest<Result<PaginatedList<AseguradoraDto>>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetAseguradorasQueryHandler(IApplicationDbContext context) : IRequestHandler<GetAseguradorasQuery, Result<PaginatedList<AseguradoraDto>>>
{
    public async Task<Result<PaginatedList<AseguradoraDto>>> Handle(GetAseguradorasQuery request, CancellationToken cancellationToken)
    {
        var query = context.Aseguradoras
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.NombreComercial.ToLower().Contains(searchTerm) ||
                                     x.CodigoIdentificadorNacional.ToLower().Contains(searchTerm));
        }

        var paginatedList = await PaginatedList<AseguradoraDto>.CreateAsync(
            query.Select(x => new AseguradoraDto(x.Id, x.NombreComercial, x.CodigoIdentificadorNacional)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
