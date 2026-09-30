using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empleados.Commands.CreateEmpleado;

public class CreateEmpleadoCommandValidator : AbstractValidator<CreateEmpleadoCommand>
{
    public CreateEmpleadoCommandValidator()
    {
        _ = RuleFor(v => v.Sucursal_Base_ID)
            .NotEmpty().WithMessage("Sucursal Base ID is required.");

        _ = RuleFor(v => v.Nombres)
            .NotEmpty().WithMessage("Nombres is required.")
            .MaximumLength(75).WithMessage("Nombres must not exceed 75 characters.");

        _ = RuleFor(v => v.Apellido_Paterno)
            .NotEmpty().WithMessage("Apellido Paterno is required.")
            .MaximumLength(75).WithMessage("Apellido Paterno must not exceed 75 characters.");

        _ = RuleFor(v => v.Apellido_Materno)
            .NotEmpty().WithMessage("Apellido Materno is required.")
            .MaximumLength(75).WithMessage("Apellido Materno must not exceed 75 characters.");

        _ = RuleFor(v => v.Licencia_Prof)
            .MaximumLength(50).WithMessage("Licencia Prof must not exceed 50 characters.");

        _ = RuleFor(v => v.Email)
            .MaximumLength(Empleado.EmailMaxLength).WithMessage("Email must not exceed 254 characters.")
            .EmailAddress().WithMessage("Email is not a valid address.")
            .When(v => !string.IsNullOrWhiteSpace(v.Email));
    }
}
