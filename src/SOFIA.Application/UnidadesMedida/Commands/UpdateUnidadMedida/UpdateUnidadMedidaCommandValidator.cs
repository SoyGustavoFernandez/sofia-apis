using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.UnidadesMedida.Commands.UpdateUnidadMedida;

public class UpdateUnidadMedidaCommandValidator : AbstractValidator<UpdateUnidadMedidaCommand>
{
    public UpdateUnidadMedidaCommandValidator()
    {
        _ = RuleFor(v => v.Id)
            .NotEmpty().WithMessage("ID is required.");

        _ = RuleFor(v => v.Codigo)
            .NotEmpty().WithMessage("Codigo is required.")
            .MaximumLength(10).WithMessage("Codigo must not exceed 10 characters.");

        _ = RuleFor(v => v.Descripcion)
            .NotEmpty().WithMessage("Descripcion is required.")
            .MaximumLength(50).WithMessage("Descripcion must not exceed 50 characters.");
    }
}
