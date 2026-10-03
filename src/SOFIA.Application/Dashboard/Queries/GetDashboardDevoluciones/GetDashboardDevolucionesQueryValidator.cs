using FluentValidation;
using SOFIA.Application.Dashboard.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardDevoluciones;

public class GetDashboardDevolucionesQueryValidator : AbstractValidator<GetDashboardDevolucionesQuery>
{
    public GetDashboardDevolucionesQueryValidator() => _ = RuleFor(x => x.Hoy)
            .Must(hoy => DashboardDias.EsHoyValido(hoy, DateTimeOffset.UtcNow))
            .WithMessage(DashboardDias.HoyFueraDeRango);
}
