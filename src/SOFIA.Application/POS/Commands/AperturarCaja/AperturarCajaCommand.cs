using System.Globalization;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Commands.AperturarCaja;

public record AperturarCajaCommand(decimal MontoAperturaEfectivo) : ICommand<Guid>, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        resultValue is Guid id ? new(AuditEventos.CajaAperturar, AuditTablas.SesionesCaja, id, $"estado: Abierta; apertura: {MontoAperturaEfectivo.ToString(CultureInfo.InvariantCulture)}") : null;
}
