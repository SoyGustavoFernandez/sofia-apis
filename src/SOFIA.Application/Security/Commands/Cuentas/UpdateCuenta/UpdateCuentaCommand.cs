using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Security.Commands.Cuentas.UpdateCuenta;

public record UpdateCuentaCommand(
    Guid Id,
    bool? CuentaActiva,
    bool? ForzarCambioClave,
    bool ResetearIntentos) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.CuentaActualizar, AuditTablas.Cuentas, Id, $"activa: {CuentaActiva?.ToString() ?? "-"}; forzarCambioClave: {ForzarCambioClave?.ToString() ?? "-"}; resetearIntentos: {ResetearIntentos}");
}
