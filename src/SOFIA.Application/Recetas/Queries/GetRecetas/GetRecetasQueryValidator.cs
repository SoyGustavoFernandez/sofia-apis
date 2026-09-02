using FluentValidation;

namespace SOFIA.Application.Recetas.Queries.GetRecetas;

public class GetRecetasQueryValidator : AbstractValidator<GetRecetasQuery>
{
    public GetRecetasQueryValidator() => RuleFor(x => x.ClienteId)
            .NotEqual(Guid.Empty)
            .When(x => x.ClienteId.HasValue);
}
