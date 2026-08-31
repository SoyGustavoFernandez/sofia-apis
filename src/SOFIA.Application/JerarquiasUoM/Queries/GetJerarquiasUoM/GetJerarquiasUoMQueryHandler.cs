using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Queries.GetJerarquiasUoM;

public class GetJerarquiasUoMQueryHandler(IApplicationDbContext context) : IRequestHandler<GetJerarquiasUoMQuery, Result<PaginatedList<JerarquiaUoMDto>>>
{
    public async Task<Result<PaginatedList<JerarquiaUoMDto>>> Handle(GetJerarquiasUoMQuery request, CancellationToken cancellationToken)
    {
        var query = context.JerarquiasUoM
            .AsNoTracking()
            .Include(x => x.Producto)
            .Include(x => x.UnidadMayor)
            .Include(x => x.UnidadMenor)
            .Where(x => !x.IsDeleted);

        if (request.ProductoId.HasValue)
        {
            query = query.Where(x => x.ProductoId == request.ProductoId.Value);
        }

        var paginatedList = await PaginatedList<JerarquiaUoMDto>.CreateAsync(
            query.Select(x => new JerarquiaUoMDto(
                x.Id,
                x.ProductoId,
                x.Producto != null ? x.Producto.NombreComercial : "Unknown",
                x.UnidadMayorId,
                x.UnidadMayor != null ? x.UnidadMayor.Descripcion : "Unknown",
                x.UnidadMenorId,
                x.UnidadMenor != null ? x.UnidadMenor.Descripcion : "Unknown",
                x.Multiplicador)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
