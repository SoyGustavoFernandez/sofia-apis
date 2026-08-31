using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.ValueObjects;

namespace SOFIA.Application.Empresas.Commands.UpdateEmpresa;

public class UpdateEmpresaCommandValidator : AbstractValidator<UpdateEmpresaCommand>
{
    public UpdateEmpresaCommandValidator()
    {
        _ = RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200);

        _ = When(x => x.RUC is not null, () =>
            RuleFor(x => x.RUC)
                .Length(11).WithMessage("RUC must be exactly 11 digits.")
                .Matches("^[0-9]{11}$").WithMessage("RUC must contain only digits."));
    }
}
