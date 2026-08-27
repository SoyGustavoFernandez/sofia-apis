using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.UnidadesMedida.Commands.CreateUnidadMedida;

public record CreateUnidadMedidaCommand(string Codigo, string Descripcion) : ICommand<Guid>;

public class CreateUnidadMedidaCommandValidator : AbstractValidator<CreateUnidadMedidaCommand>
{
    public CreateUnidadMedidaCommandValidator()
    {
        _ = RuleFor(v => v.Codigo)
            .NotEmpty().WithMessage("Codigo is required.")
            .MaximumLength(10).WithMessage("Codigo must not exceed 10 characters.");

        _ = RuleFor(v => v.Descripcion)
            .NotEmpty().WithMessage("Descripcion is required.")
            .MaximumLength(50).WithMessage("Descripcion must not exceed 50 characters.");
    }
}

public class CreateUnidadMedidaCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateUnidadMedidaCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateUnidadMedidaCommand request, CancellationToken cancellationToken)
    {
        var result = UnidadMedida.Create(request.Codigo, request.Descripcion);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.UnidadesMedida.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
