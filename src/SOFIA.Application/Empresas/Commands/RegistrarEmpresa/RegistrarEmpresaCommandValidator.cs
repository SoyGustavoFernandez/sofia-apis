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

        _ = RuleFor(x => x.RUC)
            .NotEmpty().WithMessage("RUC is required.")
            .Length(11).WithMessage("RUC must be exactly 11 digits.")
            .Matches("^[0-9]{11}$").WithMessage("RUC must contain only digits.");

        _ = RuleFor(x => x.AdminNombres)
            .NotEmpty().WithMessage("Admin first names are required.")
            .MaximumLength(75).WithMessage("Admin first names must not exceed 75 characters.");

        _ = RuleFor(x => x.AdminApellidoPaterno)
            .NotEmpty().WithMessage("Admin paternal surname is required.")
            .MaximumLength(75).WithMessage("Admin paternal surname must not exceed 75 characters.");

        _ = RuleFor(x => x.AdminApellidoMaterno)
            .MaximumLength(75).WithMessage("Admin maternal surname must not exceed 75 characters.");

        _ = RuleFor(x => x.AdminEmail)
            .NotEmpty().WithMessage("Admin email is required.")
            .MaximumLength(Empleado.EmailMaxLength).WithMessage("Admin email must not exceed 254 characters.")
            .EmailAddress().WithMessage("Admin email is not a valid address.");

        _ = RuleFor(x => x.DireccionSede)
            .NotEmpty().WithMessage("Main branch address is required.")
            .MaximumLength(255).WithMessage("Main branch address must not exceed 255 characters.");
    }
}
