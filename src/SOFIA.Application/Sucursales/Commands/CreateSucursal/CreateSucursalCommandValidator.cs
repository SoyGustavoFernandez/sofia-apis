using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Sucursales.Commands.CreateSucursal;

public class CreateSucursalCommandValidator : AbstractValidator<CreateSucursalCommand>
{
    public CreateSucursalCommandValidator()
    {
        _ = RuleFor(v => v.Nombre)
            .NotEmpty().WithMessage("Nombre is required.")
            .MaximumLength(100).WithMessage("Nombre must not exceed 100 characters.");

        _ = RuleFor(v => v.DireccionFisica)
            .NotEmpty().WithMessage("Direccion Fisica is required.")
            .MaximumLength(255).WithMessage("Direccion Fisica must not exceed 255 characters.");

        _ = RuleFor(v => v.NumeroLicencia)
            .NotEmpty().WithMessage("Numero Licencia is required.")
            .MaximumLength(50).WithMessage("Numero Licencia must not exceed 50 characters.");
    }
}
