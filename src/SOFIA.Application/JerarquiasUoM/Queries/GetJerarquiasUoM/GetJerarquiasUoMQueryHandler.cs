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
        // Producto, UnidadMayor and UnidadMenor are required; a row whose principal was soft-deleted is invalid data and is excluded.
        var query = context.JerarquiasUoM
            .AsNoTracking()
            .Where(x => !x.IsDeleted && x.Producto != null && x.UnidadMayor != null && x.UnidadMenor != null);

        if (request.ProductoId.HasValue)
        {
            query = query.Where(x => x.ProductoId == request.ProductoId.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.ProductoNombre))
        {
            var term = request.ProductoNombre.ToLower();
            query = query.Where(x => x.Producto!.NombreComercial.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.UnidadMayorNombre))
        {
            var term = request.UnidadMayorNombre.ToLower();
            query = query.Where(x => x.UnidadMayor!.Descripcion.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.UnidadMenorNombre))
        {
            var term = request.UnidadMenorNombre.ToLower();
            query = query.Where(x => x.UnidadMenor!.Descripcion.ToLower().Contains(term));
        }

        if (request.MultiplicadorMin.HasValue)
        {
            query = query.Where(x => x.Multiplicador >= request.MultiplicadorMin.Value);
        }

        if (request.MultiplicadorMax.HasValue)
        {
            query = query.Where(x => x.Multiplicador <= request.MultiplicadorMax.Value);
        }

        var paginatedList = await PaginatedList<JerarquiaUoMDto>.CreateAsync(
            query.OrderBy(x => x.Producto!.NombreComercial)
                .Select(x => new JerarquiaUoMDto(
                    x.Id,
                    x.ProductoId,
                    x.Producto!.NombreComercial,
                    x.UnidadMayorId,
                    x.UnidadMayor!.Descripcion,
                    x.UnidadMenorId,
                    x.UnidadMenor!.Descripcion,
                    x.Multiplicador)),
            request.PageNumber,
            request.PageSize);

        return Result.Success(paginatedList);
    }
}
