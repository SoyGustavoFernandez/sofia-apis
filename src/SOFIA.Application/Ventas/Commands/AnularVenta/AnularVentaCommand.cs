using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Ventas.Commands.AnularVenta;

public record AnularVentaCommand(Guid VentaId, string Motivo) : ICommand, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.VentaAnular, AuditTablas.Ventas, VentaId, "estado: Anulada");
}
