using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.DeleteCuenta;

public record DeleteCuentaCommand(Guid Id) : IRequest<Result>;
