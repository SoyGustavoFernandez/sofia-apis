using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Profesionales.Commands.CreateProfesionalSalud;

public class CreateProfesionalSaludCommandValidator : AbstractValidator<CreateProfesionalSaludCommand>
{
    public CreateProfesionalSaludCommandValidator()
    {
        _ = RuleFor(v => v.NumeroRegistro)
            .NotEmpty().WithMessage("NÃºmero de Registro is required.")
            .MaximumLength(50).WithMessage("NÃºmero de Registro must not exceed 50 characters.");

        _ = RuleFor(v => v.NombrePrescriptor)
            .NotEmpty().WithMessage("Nombre de Prescriptor is required.")
            .MaximumLength(150).WithMessage("Nombre de Prescriptor must not exceed 150 characters.");

        _ = RuleFor(v => v.DireccionClinica)
            .MaximumLength(255).WithMessage("DirecciÃ³n de la ClÃ­nica must not exceed 255 characters.");
    }
}
