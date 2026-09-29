using System.Globalization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.POS.Commands.CerrarCaja;

public record CerrarCajaCommand(Guid SesionId, decimal MontoCierre) : ICommand<Guid>, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.CajaCerrar, AuditTablas.SesionesCaja, SesionId, $"cierre desde Abierta; declarado: {MontoCierre.ToString(CultureInfo.InvariantCulture)}");
}
