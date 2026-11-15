using FluentValidation;

namespace SOFIA.Application.Devoluciones.Queries.GetDevoluciones;

public class GetDevolucionesQueryValidator : AbstractValidator<GetDevolucionesQuery>
{
    public GetDevolucionesQueryValidator()
    {
        _ = RuleFor(x => x.EmpleadoAutorizaId)
            .NotEqual(Guid.Empty)
            .When(x => x.EmpleadoAutorizaId.HasValue);

        _ = RuleFor(x => x.MotivoSunatCatalogo)
            .Length(2).WithMessage("The SUNAT reason must be exactly 2 characters.")
            .When(x => !string.IsNullOrWhiteSpace(x.MotivoSunatCatalogo));

        _ = RuleFor(x => x.NumeroNotaCredito)
            .MaximumLength(20);

        _ = RuleFor(x => x.MontoMin)
            .GreaterThanOrEqualTo(0)
            .When(x => x.MontoMin.HasValue);

        _ = RuleFor(x => x.MontoMax)
            .GreaterThanOrEqualTo(x => x.MontoMin ?? 0)
            .When(x => x.MontoMax.HasValue);
    }
}
