using MediatR;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.UpdateCuenta;

public record UpdateCuentaCommand(
    Guid Id,
    bool? CuentaActiva,
    bool? ForzarCambioClave,
    bool ResetearIntentos) : IRequest<Result>;
