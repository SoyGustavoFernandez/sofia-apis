using FluentValidation;

namespace SOFIA.Application.Magistrales.Queries.GetOrdenes;

public class GetOrdenesQueryValidator : AbstractValidator<GetOrdenesQuery>
{
    public GetOrdenesQueryValidator() => RuleFor(x => x.SucursalId)
            .NotEqual(Guid.Empty)
            .When(x => x.SucursalId.HasValue);
}
