using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;

namespace SOFIA.Application.Sucursales.Commands.UpdateSucursal;

public record UpdateSucursalCommand : ICommand
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string DireccionFisica { get; init; } = string.Empty;
    public string NumeroLicencia { get; init; } = string.Empty;
    public Guid? GerenteId { get; init; }
}
