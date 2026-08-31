using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Profesionales.Commands.CreateProfesionalSalud;

public record CreateProfesionalSaludCommand(string NumeroRegistro, string NombrePrescriptor, string? DireccionClinica) : ICommand<Guid>;

public class CreateProfesionalSaludCommandValidator : AbstractValidator<CreateProfesionalSaludCommand>
{
    public CreateProfesionalSaludCommandValidator()
    {
        _ = RuleFor(v => v.NumeroRegistro)
            .NotEmpty().WithMessage("Número de Registro is required.")
            .MaximumLength(50).WithMessage("Número de Registro must not exceed 50 characters.");

        _ = RuleFor(v => v.NombrePrescriptor)
            .NotEmpty().WithMessage("Nombre de Prescriptor is required.")
            .MaximumLength(150).WithMessage("Nombre de Prescriptor must not exceed 150 characters.");

        _ = RuleFor(v => v.DireccionClinica)
            .MaximumLength(255).WithMessage("Dirección de la Clínica must not exceed 255 characters.");
    }
}

public class CreateProfesionalSaludCommandHandler(IApplicationDbContext context) : IRequestHandler<CreateProfesionalSaludCommand, Result<Guid>>
{
    public async Task<Result<Guid>> Handle(CreateProfesionalSaludCommand request, CancellationToken cancellationToken)
    {
        var result = ProfesionalSalud.Create(request.NumeroRegistro, request.NombrePrescriptor, request.DireccionClinica);

        if (!result.IsSuccess)
        {
            return Result.Failure<Guid>(result.Error);
        }

        _ = context.ProfesionalesSalud.Add(result.Value);

        _ = await context.SaveChangesAsync(cancellationToken);

        return Result.Success(result.Value.Id, 201);
    }
}
