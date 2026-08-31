using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Laboratorios.Commands.CreateLaboratorio;

public class CreateLaboratorioCommandValidator : AbstractValidator<CreateLaboratorioCommand>
{
    public CreateLaboratorioCommandValidator()
    {
        _ = RuleFor(v => v.NombreCompania)
            .NotEmpty().WithMessage("Nombre de CompaÃ±Ã­a is required.")
            .MaximumLength(150).WithMessage("Nombre de CompaÃ±Ã­a must not exceed 150 characters.");

        _ = RuleFor(v => v.CodigoIdentificador)
            .MaximumLength(50).WithMessage("CÃ³digo Identificador must not exceed 50 characters.");
    }
}
