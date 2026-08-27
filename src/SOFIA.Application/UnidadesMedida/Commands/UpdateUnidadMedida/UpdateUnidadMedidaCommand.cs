using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Commands.UpdateUnidadMedida;

public record UpdateUnidadMedidaCommand(Guid Id, string Codigo, string Descripcion) : ICommand;

public class UpdateUnidadMedidaCommandValidator : AbstractValidator<UpdateUnidadMedidaCommand>
{
    public UpdateUnidadMedidaCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.Codigo)
            .NotEmpty().WithMessage("Codigo is required.")
            .MaximumLength(10).WithMessage("Codigo must not exceed 10 characters.");

        _ = RuleFor(v => v.Descripcion)
            .NotEmpty().WithMessage("Descripcion is required.")
            .MaximumLength(50).WithMessage("Descripcion must not exceed 50 characters.");
    }
}

public class UpdateUnidadMedidaCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateUnidadMedidaCommand, Result>
{
    public async Task<Result> Handle(UpdateUnidadMedidaCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.UnidadesMedida
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("UnidadMedida.NotFound", $"Unidad de Medida with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(request.Codigo, request.Descripcion);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
