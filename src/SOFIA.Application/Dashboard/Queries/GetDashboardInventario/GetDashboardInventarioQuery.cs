using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardInventario;

// Hoy is the instant where the local day of the caller starts
public record GetDashboardInventarioQuery(DateTimeOffset Hoy) : IRequest<Result<DashboardInventarioDto>>;
