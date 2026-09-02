using FluentValidation;

namespace SOFIA.Application.Servicios.Queries.GetInmunizaciones;

public class GetInmunizacionesQueryValidator : AbstractValidator<GetInmunizacionesQuery>
{
    public GetInmunizacionesQueryValidator() => RuleFor(x => x.ClienteId)
            .NotEqual(Guid.Empty)
            .When(x => x.ClienteId.HasValue);
}
