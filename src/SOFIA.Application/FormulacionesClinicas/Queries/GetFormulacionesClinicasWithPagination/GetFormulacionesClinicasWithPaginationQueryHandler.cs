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

        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            query = query.Where(x =>
                (x.Producto != null && x.Producto.NombreComercial.Contains(request.SearchTerm)) ||
                (x.Ingrediente != null && x.Ingrediente.DenominacionDci.Contains(request.SearchTerm)) ||
                x.UnidadDosisClinica.Contains(request.SearchTerm));
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
                UnidadDosisClinica = x.UnidadDosisClinica,
                CodigoTeOrange = x.CodigoTeOrange
            })
            .PaginatedListAsync(request.PageNumber, request.PageSize);

        return Result.Success(paginatedList);
    }
}
