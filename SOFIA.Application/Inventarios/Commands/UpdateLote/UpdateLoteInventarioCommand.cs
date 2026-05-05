using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Inventarios.Commands.UpdateLote;

public record UpdateLoteInventarioCommand(
    Guid Id,
    string NumeroLoteMfr,
    DateTimeOffset? FechaFabricacion,
    DateTimeOffset FechaCaducidad) : IRequest<Result>;

public class UpdateLoteInventarioValidator : AbstractValidator<UpdateLoteInventarioCommand>
{
    public UpdateLoteInventarioValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.NumeroLoteMfr)
            .NotEmpty().WithMessage("Batch number (Mfr) is required.")
            .MaximumLength(100).WithMessage("Batch number must not exceed 100 characters.");

        _ = RuleFor(v => v.FechaCaducidad)
            .NotEmpty().WithMessage("Expiration date is required.")
            .GreaterThan(DateTimeOffset.UtcNow).WithMessage("Expiration date must be in the future.");

        _ = RuleFor(v => v.FechaFabricacion)
            .LessThanOrEqualTo(DateTimeOffset.UtcNow).WithMessage("Manufacture date cannot be in the future.")
            .When(v => v.FechaFabricacion.HasValue);
    }
}

public class UpdateLoteInventarioHandler(IApplicationDbContext context)
    : IRequestHandler<UpdateLoteInventarioCommand, Result>
{
    public async Task<Result> Handle(UpdateLoteInventarioCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.LotesInventario
            .FindAsync([request.Id], cancellationToken);

        if (entity is null)
        {
            return Result.Failure(Error.NotFound("LoteInventario.NotFound", "The specified batch does not exist."), 404);
        }

        var result = entity.Update(
            request.NumeroLoteMfr,
            request.FechaFabricacion,
            request.FechaCaducidad);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
