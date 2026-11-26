using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Onboarding.Queries.GetPrimerosPasos;

public record GetPrimerosPasosQuery : IRequest<Result<PrimerosPasosDto>>;
