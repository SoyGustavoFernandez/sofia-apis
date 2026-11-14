using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Plataforma.Queries.GetConfiguracionPago;

public record GetConfiguracionPagoQuery : IRequest<Result<ConfiguracionPagoDto>>;
