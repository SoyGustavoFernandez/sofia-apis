using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Laboratorios.Commands.UpdateLaboratorio;

public class UpdateLaboratorioCommandValidator : AbstractValidator<UpdateLaboratorioCommand>
{
    public UpdateLaboratorioCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.NombreCompania)
            .NotEmpty().WithMessage("Nombre de CompaÃ±Ã­a is required.")
            .MaximumLength(150).WithMessage("Nombre de CompaÃ±Ã­a must not exceed 150 characters.");

        _ = RuleFor(v => v.CodigoIdentificador)
            .MaximumLength(50).WithMessage("CÃ³digo Identificador must not exceed 50 characters.");
    }
}
