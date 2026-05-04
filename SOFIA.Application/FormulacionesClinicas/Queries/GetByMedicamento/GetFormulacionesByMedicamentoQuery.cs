using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Queries.GetByMedicamento;

public record GetFormulacionesByMedicamentoQuery(Guid ProductoId) : IRequest<Result<List<FormulacionClinicaDto>>>;

public class GetFormulacionesByMedicamentoQueryHandler(IApplicationDbContext context) : IRequestHandler<GetFormulacionesByMedicamentoQuery, Result<List<FormulacionClinicaDto>>>
{
    public async Task<Result<List<FormulacionClinicaDto>>> Handle(GetFormulacionesByMedicamentoQuery request, CancellationToken cancellationToken)
    {
        var list = await context.FormulacionesClinicas
            .AsNoTracking()
            .Where(x => x.ProductoId == request.ProductoId && !x.IsDeleted)
            .Include(x => x.Producto)
            .Include(x => x.Ingrediente)
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
            .ToListAsync(cancellationToken);

        return Result.Success(list);
    }
}
