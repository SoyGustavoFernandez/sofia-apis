using FluentValidation;

namespace SOFIA.Application.POS.Queries.GetSesiones;

public class GetSesionesQueryValidator : AbstractValidator<GetSesionesQuery>
{
    public GetSesionesQueryValidator() => RuleFor(x => x.SucursalId)
            .NotEqual(Guid.Empty)
            .When(x => x.SucursalId.HasValue);
}
