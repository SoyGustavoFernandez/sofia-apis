using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.DIGEMID.Queries.GetDigemidCatalogo;

public record DigemidProductoDto(Guid Id, string CodProd, string NomProd, string? Concent, string? FormaFarmaceutica, string? RegistroSanitario, string? Titular, string Estado);

public record GetDigemidCatalogoQuery : IRequest<Result<PaginatedList<DigemidProductoDto>>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetDigemidCatalogoQueryHandler(IApplicationDbContext context) : IRequestHandler<GetDigemidCatalogoQuery, Result<PaginatedList<DigemidProductoDto>>>
{
    public async Task<Result<PaginatedList<DigemidProductoDto>>> Handle(GetDigemidCatalogoQuery request, CancellationToken cancellationToken)
    {
        var query = context.DigemidCatalogoProductos
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.CodProd.ToLower().Contains(searchTerm) ||
                                     x.NomProd.ToLower().Contains(searchTerm));
        }

        var paginatedList = await PaginatedList<DigemidProductoDto>.CreateAsync(
            query.Select(x => new DigemidProductoDto(x.Id, x.CodProd, x.NomProd, x.Concent, x.FormaFarmaceutica, x.RegistroSanitario, x.Titular, x.Estado)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
