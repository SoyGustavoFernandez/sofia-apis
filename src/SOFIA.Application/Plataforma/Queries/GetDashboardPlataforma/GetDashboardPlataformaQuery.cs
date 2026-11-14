using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Plataforma.Queries.GetDashboardPlataforma;

/// <summary>InicioMes is the instant where the operator's local month starts; null falls back to the UTC month.</summary>
public record GetDashboardPlataformaQuery(DateTimeOffset? InicioMes) : IRequest<Result<DashboardPlataformaDto>>;
