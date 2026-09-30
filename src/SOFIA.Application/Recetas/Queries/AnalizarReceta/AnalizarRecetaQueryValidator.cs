using FluentValidation;

namespace SOFIA.Application.Recetas.Queries.AnalizarReceta;

public class AnalizarRecetaQueryValidator : AbstractValidator<AnalizarRecetaQuery>
{
    public const int MaxEspecialidadContextoLength = 50;

    public AnalizarRecetaQueryValidator() =>
        // The specialty is interpolated into the AI prompt, so only a short plain-text name is accepted
        _ = RuleFor(x => x.EspecialidadContexto)
            .MaximumLength(MaxEspecialidadContextoLength)
            .Matches(@"^[\p{L} ]*$")
            .When(x => !string.IsNullOrEmpty(x.EspecialidadContexto));
}
