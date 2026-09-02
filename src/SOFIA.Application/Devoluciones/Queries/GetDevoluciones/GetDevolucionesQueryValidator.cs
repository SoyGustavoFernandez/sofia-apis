using FluentValidation;

namespace SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

public class GetDevolucionesQueryValidator : AbstractValidator<GetDevolucionesQuery>
{
    public GetDevolucionesQueryValidator() => RuleFor(x => x.EmpleadoAutorizaId)
            .NotEqual(Guid.Empty)
            .When(x => x.EmpleadoAutorizaId.HasValue);
}
