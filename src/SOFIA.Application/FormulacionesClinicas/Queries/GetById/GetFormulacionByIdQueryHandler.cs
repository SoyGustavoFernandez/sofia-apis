using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Queries.GetById;

public class GetFormulacionByIdQueryHandler(IApplicationDbContext context) : IRequestHandler<GetFormulacionByIdQuery, Result<FormulacionClinicaDto>>
{
    public async Task<Result<FormulacionClinicaDto>> Handle(GetFormulacionByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await context.FormulacionesClinicas
            .AsNoTracking()
            .Where(x => x.Id == request.Id && !x.IsDeleted)
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
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null
            ? Result.Failure<FormulacionClinicaDto>(Error.NotFound("Formulacion.NotFound", $"FormulaciÃ³n with ID {request.Id} not found."))
            : Result.Success(dto);
    }
}
