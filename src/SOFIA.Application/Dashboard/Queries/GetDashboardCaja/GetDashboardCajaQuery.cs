using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardCaja;

public record GetDashboardCajaQuery : IRequest<Result<DashboardCajaDto>>;
