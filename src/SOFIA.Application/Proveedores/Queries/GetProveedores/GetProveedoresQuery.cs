using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Queries.GetProveedores;

public record ProveedorDto(Guid Id, string RazonSocial, string TaxId, string? TerminosFinancieros, decimal? CalificacionEsg, decimal TasaCumplimiento);

public record GetProveedoresQuery : IRequest<Result<PaginatedList<ProveedorDto>>>
{
    public string? SearchTerm { get; init; }
    public int PageNumber { get; init; } = 1;
    public int PageSize { get; init; } = 10;
}

public class GetProveedoresQueryHandler(IApplicationDbContext context) : IRequestHandler<GetProveedoresQuery, Result<PaginatedList<ProveedorDto>>>
{
    public async Task<Result<PaginatedList<ProveedorDto>>> Handle(GetProveedoresQuery request, CancellationToken cancellationToken)
    {
        var query = context.Proveedores
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            var searchTerm = request.SearchTerm.ToLower();
            query = query.Where(x => x.RazonSocial.ToLower().Contains(searchTerm) ||
                                     x.TaxId.ToLower().Contains(searchTerm));
        }

        var paginatedList = await PaginatedList<ProveedorDto>.CreateAsync(
            query.Select(x => new ProveedorDto(x.Id, x.RazonSocial, x.TaxId, x.TerminosFinancieros, x.CalificacionEsg, x.TasaCumplimiento)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
