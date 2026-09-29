using System.Globalization;
using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Inventarios.Commands.RegisterInventario;

public record RegisterInventarioCommand(
    Guid SucursalId,
    Guid LoteId,
    decimal Cantidad,
    bool EsAjusteDirecto = false) : ICommand<Guid>, IAuditableCommand
{
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        EsAjusteDirecto && resultValue is Guid id ? new(AuditEventos.StockAjustar, AuditTablas.Inventario, id, $"ajusteDirecto; nuevaCantidad: {Cantidad.ToString(CultureInfo.InvariantCulture)}") : null;
}
