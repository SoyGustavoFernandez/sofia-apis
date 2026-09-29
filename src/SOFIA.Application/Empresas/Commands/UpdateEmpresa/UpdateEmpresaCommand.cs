using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Application.Common.Models;
using SOFIA.Domain.Common;
using SOFIA.Domain.ValueObjects;

namespace SOFIA.Application.Empresas.Commands.UpdateEmpresa;

public record UpdateEmpresaCommand : ICommand, IAuditableCommand
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? RUC { get; init; }

    // RUC 10 embeds a person's DNI, so neither the name nor the RUC goes into the audit detail
    public AuditEntry? GetAuditEntry(object? resultValue) =>
        new(AuditEventos.EmpresaActualizar, AuditTablas.Empresas, Id);
}
