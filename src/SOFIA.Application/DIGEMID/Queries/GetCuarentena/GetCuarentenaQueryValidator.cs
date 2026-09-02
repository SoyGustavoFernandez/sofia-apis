using FluentValidation;

namespace SOFIA.Application.DIGEMID.Queries.GetCuarentena;

public class GetCuarentenaQueryValidator : AbstractValidator<GetCuarentenaQuery>
{
    public GetCuarentenaQueryValidator() => RuleFor(x => x.SucursalId)
            .NotEqual(Guid.Empty)
            .When(x => x.SucursalId.HasValue);
}
