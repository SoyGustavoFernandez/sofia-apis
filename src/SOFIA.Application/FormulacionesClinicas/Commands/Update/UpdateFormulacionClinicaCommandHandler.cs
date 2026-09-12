using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Commands.Update;

public class UpdateFormulacionClinicaCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateFormulacionClinicaCommand, Result>
{
    public async Task<Result> Handle(UpdateFormulacionClinicaCommand request, CancellationToken cancellationToken)
    {
        var formulacion = await context.FormulacionesClinicas
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (formulacion is null)
        {
            return Result.Failure(Error.NotFound("Formulacion.NotFound", $"FormulaciÃ³n with ID {request.Id} not found."));
        }

        // Check that the ingredient exists (only if it changed)
        if (formulacion.IngredienteId != request.IngredienteId)
        {
            var ingredienteExists = await context.IngredientesActivos
                .AnyAsync(x => x.Id == request.IngredienteId && !x.IsDeleted, cancellationToken);

            if (!ingredienteExists)
            {
                return Result.Failure(Error.NotFound("IngredienteActivo.NotFound", $"Ingrediente Activo with ID {request.IngredienteId} not found."));
            }
        }

        // Check that the unit of measure exists (only if it changed)
        if (formulacion.UnidadMedidaId != request.UnidadMedidaId)
        {
            var unidadMedidaExists = await context.UnidadesMedida
                .AnyAsync(x => x.Id == request.UnidadMedidaId && !x.IsDeleted, cancellationToken);

            if (!unidadMedidaExists)
            {
                return Result.Failure(Error.NotFound("UnidadMedida.NotFound", $"Unidad de Medida with ID {request.UnidadMedidaId} not found."));
            }
        }

        var result = formulacion.Update(
            request.IngredienteId,
            request.ConcentracionDosis,
            request.UnidadMedidaId,
            request.CodigoTeOrange);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
