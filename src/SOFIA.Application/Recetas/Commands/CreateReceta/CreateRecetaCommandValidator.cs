using FluentValidation;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Recetas.Commands.CreateReceta;

public class CreateRecetaCommandValidator : AbstractValidator<CreateRecetaCommand>
{
    public CreateRecetaCommandValidator()
    {
        _ = RuleFor(x => x.ClienteId).NotEmpty();
        _ = RuleFor(x => x.MedicoId).NotEmpty();
        _ = RuleFor(x => x.FechaExpedicion).NotEqual(default(DateOnly));
        _ = RuleFor(x => x.RepeticionesMax).GreaterThanOrEqualTo(0);
        _ = RuleFor(x => x.IndicacionesUso).MaximumLength(1000).When(x => x.IndicacionesUso != null);
        _ = RuleFor(x => x.NumeroReceta).MaximumLength(RecetaMedica.NumeroRecetaMaxLength).When(x => x.NumeroReceta != null);
    }
}
