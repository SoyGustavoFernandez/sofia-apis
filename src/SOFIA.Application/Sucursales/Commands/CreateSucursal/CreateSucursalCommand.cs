using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Sucursales.Commands.CreateSucursal;

public record CreateSucursalCommand : ICommand<Guid>
{
    public string Nombre { get; init; } = string.Empty;
    public string DireccionFisica { get; init; } = string.Empty;
    public string NumeroLicencia { get; init; } = string.Empty;
    public Guid? GerenteId { get; init; }
}
