using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.JerarquiasUoM.Commands.UpdateJerarquiaUoM;

public record UpdateJerarquiaUoMCommand(
    Guid Id,
    Guid ProductoId,
    Guid UnidadMayorId,
    Guid UnidadMenorId,
    decimal Multiplicador) : IRequest<Result>;

public class UpdateJerarquiaUoMCommandValidator : AbstractValidator<UpdateJerarquiaUoMCommand>
{
    public UpdateJerarquiaUoMCommandValidator()
    {
        _ = RuleFor(v => v.Id).NotEmpty();
        _ = RuleFor(v => v.ProductoId).NotEmpty();
        _ = RuleFor(v => v.UnidadMayorId).NotEmpty();
        _ = RuleFor(v => v.UnidadMenorId).NotEmpty();
        _ = RuleFor(v => v.Multiplicador).GreaterThan(0);
        _ = RuleFor(v => v).Must(x => x.UnidadMayorId != x.UnidadMenorId)
            .WithMessage("Unidad Mayor and Unidad Menor cannot be the same.");
    }
}

public class UpdateJerarquiaUoMCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateJerarquiaUoMCommand, Result>
{
    public async Task<Result> Handle(UpdateJerarquiaUoMCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.JerarquiasUoM
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("JerarquiaUoM.NotFound", $"Jerarquía with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(
            request.ProductoId,
            request.UnidadMayorId,
            request.UnidadMenorId,
            request.Multiplicador);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
