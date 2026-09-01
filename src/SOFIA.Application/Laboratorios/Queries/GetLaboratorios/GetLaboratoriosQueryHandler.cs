using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Queries.GetLaboratorios;

public class GetLaboratoriosQueryHandler(IApplicationDbContext context) : IRequestHandler<GetLaboratoriosQuery, Result<PaginatedList<LaboratorioDto>>>
{
    public async Task<Result<PaginatedList<LaboratorioDto>>> Handle(GetLaboratoriosQuery request, CancellationToken cancellationToken)
    {
        var query = context.Laboratorios
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.NombreCompania))
        {
            query = query.Where(x => x.NombreCompania.ToLower().Contains(request.NombreCompania.ToLower()));
        }

        if (!string.IsNullOrWhiteSpace(request.CodigoIdentificador))
        {
            query = query.Where(x => x.CodigoIdentificador != null && x.CodigoIdentificador.ToLower().Contains(request.CodigoIdentificador.ToLower()));
        }

        var paginatedList = await PaginatedList<LaboratorioDto>.CreateAsync(
            query.Select(x => new LaboratorioDto(x.Id, x.NombreCompania, x.CodigoIdentificador)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
