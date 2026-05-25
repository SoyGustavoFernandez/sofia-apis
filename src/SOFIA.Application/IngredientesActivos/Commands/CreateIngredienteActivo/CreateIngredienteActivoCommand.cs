using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.IngredientesActivos.Commands.CreateIngredienteActivo;

public record CreateIngredienteActivoCommand(string DenominacionDci, string CodigoAtc) : IRequest<Result<Guid>>;

public class CreateIngredienteActivoCommandValidator : AbstractValidator<CreateIngredienteActivoCommand>
{
    public CreateIngredienteActivoCommandValidator()
    {
        _ = RuleFor(v => v.DenominacionDci)
            .NotEmpty().WithMessage("Denominación DCI is required.")
            .MaximumLength(255).WithMessage("Denominación DCI must not exceed 255 characters.");

        _ = RuleFor(v => v.CodigoAtc)
            .NotEmpty().WithMessage("Código ATC is required.")
            .MaximumLength(15).WithMessage("Código ATC must not exceed 15 characters.");
    }
}

public class CreateIngredienteActivoCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateIngredienteActivoCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateIngredienteActivoCommand request, CancellationToken cancellationToken)
    {
        var result = IngredienteActivo.Create(request.DenominacionDci, request.CodigoAtc);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.IngredientesActivos.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
