using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Dashboard.Queries.GetDashboardCuarentena;

public record GetDashboardCuarentenaQuery : IRequest<Result<DashboardCuarentenaDto>>;
