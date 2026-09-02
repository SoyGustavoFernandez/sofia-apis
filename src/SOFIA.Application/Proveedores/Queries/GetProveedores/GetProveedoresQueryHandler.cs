using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Proveedores.Queries.GetProveedores;

public class GetProveedoresQueryHandler(IApplicationDbContext context) : IRequestHandler<GetProveedoresQuery, Result<PaginatedList<ProveedorDto>>>
{
    public async Task<Result<PaginatedList<ProveedorDto>>> Handle(GetProveedoresQuery request, CancellationToken cancellationToken)
    {
        var query = context.Proveedores
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.RazonSocial))
        {
            query = query.Where(x => x.RazonSocial.Contains(request.RazonSocial));
        }

        if (!string.IsNullOrWhiteSpace(request.TaxId))
        {
            query = query.Where(x => x.TaxId.Contains(request.TaxId));
        }

        if (request.TasaCumplimientoDesde.HasValue)
        {
            query = query.Where(x => x.TasaCumplimiento >= request.TasaCumplimientoDesde.Value);
        }

        if (request.TasaCumplimientoHasta.HasValue)
        {
            query = query.Where(x => x.TasaCumplimiento <= request.TasaCumplimientoHasta.Value);
        }

        var paginatedList = await PaginatedList<ProveedorDto>.CreateAsync(
            query.Select(x => new ProveedorDto(x.Id, x.RazonSocial, x.TaxId, x.TerminosFinancieros, x.CalificacionEsg, x.TasaCumplimiento)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
