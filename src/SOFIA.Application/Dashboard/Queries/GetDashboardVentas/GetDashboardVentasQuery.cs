using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardVentas;

// Hoy is the instant where the local day of the caller starts
public record GetDashboardVentasQuery(DateTimeOffset Hoy) : IRequest<Result<DashboardVentasDto>>;
