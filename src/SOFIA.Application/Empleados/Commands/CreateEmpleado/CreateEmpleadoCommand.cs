using FluentValidation;
using MediatR;
using SOFIA.Application.Common.Interfaces;
using SOFIA.Domain.Common;
using SOFIA.Domain.Entities;

namespace SOFIA.Application.Empleados.Commands.CreateEmpleado;

public record CreateEmpleadoCommand : ICommand<Guid>
{
    public Guid Sucursal_Base_ID { get; init; }
    public string Nombres { get; init; } = string.Empty;
    public string Apellido_Paterno { get; init; } = string.Empty;
    public string Apellido_Materno { get; init; } = string.Empty;
    public string? Licencia_Prof { get; init; }
    public byte[]? Huella_Biometrica { get; init; }
}
