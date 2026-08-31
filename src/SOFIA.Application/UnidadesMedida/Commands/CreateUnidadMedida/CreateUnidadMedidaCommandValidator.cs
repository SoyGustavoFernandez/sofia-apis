using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.UnidadesMedida.Commands.CreateUnidadMedida;

public class CreateUnidadMedidaCommandValidator : AbstractValidator<CreateUnidadMedidaCommand>
{
    public CreateUnidadMedidaCommandValidator()
    {
        _ = RuleFor(v => v.Codigo)
            .NotEmpty().WithMessage("Codigo is required.")
            .MaximumLength(10).WithMessage("Codigo must not exceed 10 characters.");

        _ = RuleFor(v => v.Descripcion)
            .NotEmpty().WithMessage("Descripcion is required.")
            .MaximumLength(50).WithMessage("Descripcion must not exceed 50 characters.");
    }
}
