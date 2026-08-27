using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.JerarquiasUoM.Commands.CreateJerarquiaUoM;

public record CreateJerarquiaUoMCommand(
    Guid ProductoId,
    Guid UnidadMayorId,
    Guid UnidadMenorId,
    decimal Multiplicador) : ICommand<Guid>;

public class CreateJerarquiaUoMCommandValidator : AbstractValidator<CreateJerarquiaUoMCommand>
{
    public CreateJerarquiaUoMCommandValidator()
    {
        _ = RuleFor(v => v.ProductoId).NotEmpty();
        _ = RuleFor(v => v.UnidadMayorId).NotEmpty();
        _ = RuleFor(v => v.UnidadMenorId).NotEmpty();
        _ = RuleFor(v => v.Multiplicador).GreaterThan(0);
        _ = RuleFor(v => v).Must(x => x.UnidadMayorId != x.UnidadMenorId)
            .WithMessage("Unidad Mayor and Unidad Menor cannot be the same.");
    }
}

public class CreateJerarquiaUoMCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateJerarquiaUoMCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateJerarquiaUoMCommand request, CancellationToken cancellationToken)
    {
        var result = JerarquiaUoM.Create(
            request.ProductoId,
            request.UnidadMayorId,
            request.UnidadMenorId,
            request.Multiplicador);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.JerarquiasUoM.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
