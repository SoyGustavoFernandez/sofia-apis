using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.ValueObjects;

namespace SOFIA.Application.Empresas.Commands.UpdateEmpresa;

public record UpdateEmpresaCommand : ICommand
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string? RUC { get; init; }
}
