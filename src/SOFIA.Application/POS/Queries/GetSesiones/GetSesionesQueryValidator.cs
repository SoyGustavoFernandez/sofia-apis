using FluentValidation;

namespace SOFIA.Application.POS.Queries.GetSesiones;

public class GetSesionesQueryValidator : AbstractValidator<GetSesionesQuery>
{
    public GetSesionesQueryValidator()
    {
        _ = RuleFor(x => x.SucursalId)
            .NotEqual(Guid.Empty)
            .When(x => x.SucursalId.HasValue);

        _ = RuleFor(x => x.EmpleadoId)
            .NotEqual(Guid.Empty)
            .When(x => x.EmpleadoId.HasValue);
    }
}
