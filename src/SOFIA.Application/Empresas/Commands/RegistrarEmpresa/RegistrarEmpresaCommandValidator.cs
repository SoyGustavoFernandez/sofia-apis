using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.ValueObjects;

namespace SOFIA.Application.Empresas.Commands.RegistrarEmpresa;

public class RegistrarEmpresaCommandValidator : AbstractValidator<RegistrarEmpresaCommand>
{
    public RegistrarEmpresaCommandValidator()
    {
        _ = RuleFor(x => x.NombreEmpresa)
            .NotEmpty().WithMessage("Company name is required.")
            .MaximumLength(200);

        _ = RuleFor(x => x.Usuario)
            .NotEmpty().WithMessage("Username is required.")
            .MinimumLength(4).WithMessage("Username must be at least 4 characters long.")
            .MaximumLength(50);

        _ = RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required.")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters long.");

        _ = When(x => x.RUC is not null, () =>
            RuleFor(x => x.RUC)
                .Length(11).WithMessage("RUC must be exactly 11 digits.")
                .Matches("^[0-9]{11}$").WithMessage("RUC must contain only digits."));
    }
}
