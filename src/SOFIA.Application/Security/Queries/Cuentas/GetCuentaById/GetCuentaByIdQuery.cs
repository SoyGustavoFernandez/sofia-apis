using MediatR;
using SOFIA.Application.Security;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Queries.Cuentas.GetCuentaById;

public record GetCuentaByIdQuery(Guid Id) : IRequest<Result<CuentaDto>>;
