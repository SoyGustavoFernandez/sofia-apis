using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardDevoluciones;

// Hoy is the instant where the local day of the caller starts
public record GetDashboardDevolucionesQuery(DateTimeOffset Hoy) : IRequest<Result<DashboardDevolucionesDto>>;
