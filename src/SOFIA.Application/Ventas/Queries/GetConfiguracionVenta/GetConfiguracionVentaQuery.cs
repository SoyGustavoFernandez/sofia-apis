using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Queries.GetConfiguracionVenta;

// POS lookup: company-level sale rules the cashier screen mirrors, readable with the sales permission alone.
public record GetConfiguracionVentaQuery : IRequest<Result<ConfiguracionVentaDto>>;
