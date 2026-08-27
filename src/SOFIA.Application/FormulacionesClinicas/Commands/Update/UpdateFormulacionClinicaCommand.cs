using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.FormulacionesClinicas.Commands.Update;

public record UpdateFormulacionClinicaCommand : ICommand
{
    public Guid Id { get; init; }
    public Guid IngredienteId { get; init; }
    public decimal ConcentracionDosis { get; init; }
    public string UnidadDosisClinica { get; init; } = string.Empty;
    public string? CodigoTeOrange { get; init; }
}

public class UpdateFormulacionClinicaCommandValidator : AbstractValidator<UpdateFormulacionClinicaCommand>
{
    public UpdateFormulacionClinicaCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty();
        _ = RuleFor(v => v.IngredienteId).NotEmpty();
        _ = RuleFor(v => v.ConcentracionDosis).GreaterThan(0);
        _ = RuleFor(v => v.UnidadDosisClinica).NotEmpty().MaximumLength(20);
        _ = RuleFor(v => v.CodigoTeOrange).MaximumLength(5);
    }
}

public class UpdateFormulacionClinicaCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateFormulacionClinicaCommand, Result>
{
    public async Task<Result> Handle(UpdateFormulacionClinicaCommand request, CancellationToken cancellationToken)
    {
        var formulacion = await context.FormulacionesClinicas
            .FirstOrDefaultAsync(x => x.Id == request.Id && !x.IsDeleted, cancellationToken);

        if (formulacion is null)
        {
            return Result.Failure(Error.NotFound("Formulacion.NotFound", $"Formulación with ID {request.Id} not found."));
        }

        // Verificar si el ingrediente existe (si cambió)
        if (formulacion.IngredienteId != request.IngredienteId)
        {
            var ingredienteExists = await context.IngredientesActivos
                .AnyAsync(x => x.Id == request.IngredienteId && !x.IsDeleted, cancellationToken);

            if (!ingredienteExists)
            {
                return Result.Failure(Error.NotFound("IngredienteActivo.NotFound", $"Ingrediente Activo with ID {request.IngredienteId} not found."));
            }
        }

        var result = formulacion.Update(
            request.IngredienteId,
            request.ConcentracionDosis,
            request.UnidadDosisClinica,
            request.CodigoTeOrange);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
