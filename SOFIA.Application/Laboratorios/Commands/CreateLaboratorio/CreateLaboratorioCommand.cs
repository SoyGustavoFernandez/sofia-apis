using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Laboratorios.Commands.CreateLaboratorio;

public record CreateLaboratorioCommand(string NombreCompania, string? CodigoIdentificador) : IRequest<Result<Guid>>;

public class CreateLaboratorioCommandValidator : AbstractValidator<CreateLaboratorioCommand>
{
    public CreateLaboratorioCommandValidator()
    {
        _ = RuleFor(v => v.NombreCompania)
            .NotEmpty().WithMessage("Nombre de Compañía is required.")
            .MaximumLength(150).WithMessage("Nombre de Compañía must not exceed 150 characters.");

        _ = RuleFor(v => v.CodigoIdentificador)
            .MaximumLength(50).WithMessage("Código Identificador must not exceed 50 characters.");
    }
}

public class CreateLaboratorioCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateLaboratorioCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateLaboratorioCommand request, CancellationToken cancellationToken)
    {
        var result = Laboratorio.Create(request.NombreCompania, request.CodigoIdentificador);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.Laboratorios.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
