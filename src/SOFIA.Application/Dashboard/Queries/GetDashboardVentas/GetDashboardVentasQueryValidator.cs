using FluentValidation;
using SOFIA.Application.Dashboard.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

public class GetDashboardVentasQueryValidator : AbstractValidator<GetDashboardVentasQuery>
{
    public GetDashboardVentasQueryValidator() => _ = RuleFor(x => x.Hoy)
            .Must(hoy => DashboardDias.EsHoyValido(hoy, DateTimeOffset.UtcNow))
            .WithMessage(DashboardDias.HoyFueraDeRango);
}
