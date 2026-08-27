using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Commands.UpdateLaboratorio;

public record UpdateLaboratorioCommand(Guid Id, string NombreCompania, string? CodigoIdentificador) : ICommand;

public class UpdateLaboratorioCommandValidator : AbstractValidator<UpdateLaboratorioCommand>
{
    public UpdateLaboratorioCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.NombreCompania)
            .NotEmpty().WithMessage("Nombre de Compañía is required.")
            .MaximumLength(150).WithMessage("Nombre de Compañía must not exceed 150 characters.");

        _ = RuleFor(v => v.CodigoIdentificador)
            .MaximumLength(50).WithMessage("Código Identificador must not exceed 50 characters.");
    }
}

public class UpdateLaboratorioCommandHandler(IApplicationDbContext context) : IRequestHandler<UpdateLaboratorioCommand, Result>
{
    public async Task<Result> Handle(UpdateLaboratorioCommand request, CancellationToken cancellationToken)
    {
        var entity = await context.Laboratorios
            .FindAsync([request.Id], cancellationToken);

        if (entity == null)
        {
            return Result.Failure(Error.NotFound("Laboratorio.NotFound", $"Laboratorio with ID {request.Id} was not found."), 404);
        }

        var result = entity.Update(request.NombreCompania, request.CodigoIdentificador);

        if (!result.IsSuccess)
        {
            return result;
        }

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success();
    }
}
