using FluentValidation;
using SOFIA.Application.Dashboard.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardInventario;

public class GetDashboardInventarioQueryValidator : AbstractValidator<GetDashboardInventarioQuery>
{
    public GetDashboardInventarioQueryValidator() => _ = RuleFor(x => x.Hoy)
            .Must(hoy => DashboardDias.EsHoyValido(hoy, DateTimeOffset.UtcNow))
            .WithMessage(DashboardDias.HoyFueraDeRango);
}
