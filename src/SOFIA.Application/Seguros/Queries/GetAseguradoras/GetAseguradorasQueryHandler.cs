using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Seguros.Queries.GetAseguradoras;

public class GetAseguradorasQueryHandler(IApplicationDbContext context) : IRequestHandler<GetAseguradorasQuery, Result<PaginatedList<AseguradoraDto>>>
{
    public async Task<Result<PaginatedList<AseguradoraDto>>> Handle(GetAseguradorasQuery request, CancellationToken cancellationToken)
    {
        var query = context.Aseguradoras
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.NombreComercial))
        {
            query = query.Where(x => x.NombreComercial.ToLower().Contains(request.NombreComercial.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.CodigoIdentificadorNacional))
        {
            query = query.Where(x => x.CodigoIdentificadorNacional.ToLower().Contains(request.CodigoIdentificadorNacional.ToLower()));
        }

        var paginatedList = await PaginatedList<AseguradoraDto>.CreateAsync(
            query.Select(x => new AseguradoraDto(x.Id, x.NombreComercial, x.CodigoIdentificadorNacional)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
