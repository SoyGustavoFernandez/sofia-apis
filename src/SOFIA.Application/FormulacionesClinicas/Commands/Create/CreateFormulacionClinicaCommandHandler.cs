using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.FormulacionesClinicas.Commands.Create;

public class CreateFormulacionClinicaCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateFormulacionClinicaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateFormulacionClinicaCommand request, CancellationToken cancellationToken)
    {
        // Check that the product exists
        var productoExists = await context.Medicamentos
            .AnyAsync(x => x.Id == request.ProductoId && !x.IsDeleted, cancellationToken);

        if (!productoExists)
        {
            return Result.Failure<Guid>(Error.NotFound("Medicamento.NotFound", $"Medicamento with ID {request.ProductoId} not found."));
        }

        // Check that the ingredient exists
        var ingredienteExists = await context.IngredientesActivos
            .AnyAsync(x => x.Id == request.IngredienteId && !x.IsDeleted, cancellationToken);

        if (!ingredienteExists)
        {
            return Result.Failure<Guid>(Error.NotFound("IngredienteActivo.NotFound", $"Ingrediente Activo with ID {request.IngredienteId} not found."));
        }

        // Check that the unit of measure exists
        var unidadMedidaExists = await context.UnidadesMedida
            .AnyAsync(x => x.Id == request.UnidadMedidaId && !x.IsDeleted, cancellationToken);

        if (!unidadMedidaExists)
        {
            return Result.Failure<Guid>(Error.NotFound("UnidadMedida.NotFound", $"Unidad de Medida with ID {request.UnidadMedidaId} not found."));
        }

        var result = FormulacionClinica.Create(
            request.ProductoId,
            request.IngredienteId,
            request.ConcentracionDosis,
            request.UnidadMedidaId,
            request.CodigoTeOrange);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.FormulacionesClinicas.Add(result.Value);
        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
