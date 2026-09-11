using FluentValidation;

namespace SOFIA.Application.Recetas.Commands.UpdateReceta;

public class UpdateRecetaCommandValidator : AbstractValidator<UpdateRecetaCommand>
{
    public UpdateRecetaCommandValidator()
    {
        _ = RuleFor(x => x.Id).NotEmpty();
        _ = RuleFor(x => x.ClienteId).NotEmpty();
        _ = RuleFor(x => x.MedicoId).NotEmpty();
        _ = RuleFor(x => x.FechaExpedicion).NotEqual(default(DateOnly));
        _ = RuleFor(x => x.RepeticionesMax).GreaterThanOrEqualTo(0);
        _ = RuleFor(x => x.IndicacionesUso).MaximumLength(1000).When(x => x.IndicacionesUso != null);
    }
}
