using FluentValidation;
using MediatR;
using Microsoft.EntityFrameworkCore;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;
using SOFIA.Domain.ValueObjects;

namespace SOFIA.Application.Empresas.Commands.RegistrarEmpresa;

public record RegistrarEmpresaCommand : ICommand<string>
{
    // Minimum required
    public string NombreEmpresa { get; init; } = string.Empty;
    public string Usuario { get; init; } = string.Empty;
    public string Password { get; init; } = string.Empty;

    // Optional â€” completable from the company profile after registration
    public string? RUC { get; init; }
    public string? NombreSede { get; init; }
    public string? DireccionSede { get; init; }
    public string? NumeroLicencia { get; init; }
    public string? AdminNombres { get; init; }
    public string? AdminApellidoPaterno { get; init; }
    public string? AdminApellidoMaterno { get; init; }
}
