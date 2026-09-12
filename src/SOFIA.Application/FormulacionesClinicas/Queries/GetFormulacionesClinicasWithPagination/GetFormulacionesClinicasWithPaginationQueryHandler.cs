using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Queries.GetFormulacionesClinicasWithPagination;

public class GetFormulacionesClinicasWithPaginationQueryHandler(IApplicationDbContext context)
    : IRequestHandler<GetFormulacionesClinicasWithPaginationQuery, Result<PaginatedList<FormulacionClinicaDto>>>
{
    public async Task<Result<PaginatedList<FormulacionClinicaDto>>> Handle(GetFormulacionesClinicasWithPaginationQuery request, CancellationToken cancellationToken)
    {
        var query = context.FormulacionesClinicas
            .AsNoTracking()
            .Where(x => !x.IsDeleted);

        if (!string.IsNullOrWhiteSpace(request.ProductoNombre))
        {
            var term = request.ProductoNombre.ToLower();
            query = query.Where(x => x.Producto != null && x.Producto.NombreComercial.ToLower().Contains(term));
        }

        if (!string.IsNullOrWhiteSpace(request.IngredienteNombre))
        {
            var term = request.IngredienteNombre.ToLower();
            query = query.Where(x => x.Ingrediente != null && x.Ingrediente.DenominacionDci.ToLower().Contains(term));
        }

        var paginatedList = await query
            .OrderBy(x => x.Producto != null ? x.Producto.NombreComercial : string.Empty)
            .Select(x => new FormulacionClinicaDto
            {
                Id = x.Id,
                ProductoId = x.ProductoId,
                ProductoNombre = x.Producto != null ? x.Producto.NombreComercial : null,
                IngredienteId = x.IngredienteId,
                IngredienteNombre = x.Ingrediente != null ? x.Ingrediente.DenominacionDci : null,
                ConcentracionDosis = x.ConcentracionDosis,
                UnidadMedidaId = x.UnidadMedidaId,
                UnidadMedidaNombre = x.UnidadMedida != null ? x.UnidadMedida.Descripcion : null,
                CodigoTeOrange = x.CodigoTeOrange
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
