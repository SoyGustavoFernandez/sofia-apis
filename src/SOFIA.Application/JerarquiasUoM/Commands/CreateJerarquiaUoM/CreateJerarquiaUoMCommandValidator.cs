using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.JerarquiasUoM.Commands.CreateJerarquiaUoM;

public class CreateJerarquiaUoMCommandValidator : AbstractValidator<CreateJerarquiaUoMCommand>
{
    public CreateJerarquiaUoMCommandValidator()
    {
        _ = RuleFor(v => v.ProductoId).NotEmpty();
        _ = RuleFor(v => v.UnidadMayorId).NotEmpty();
        _ = RuleFor(v => v.UnidadMenorId).NotEmpty();
        _ = RuleFor(v => v.Multiplicador).GreaterThan(0);
        _ = RuleFor(v => v).Must(x => x.UnidadMayorId != x.UnidadMenorId)
            .WithMessage("Unidad Mayor and Unidad Menor cannot be the same.");
    }
}
