namespace SOFIA.Application.Empleados;

public record EmpleadoDto
{
    public Guid Id { get; init; }
    public Guid Sucursal_Base_ID { get; init; }
    public string Nombres { get; init; } = string.Empty;
    public string Apellido_Paterno { get; init; } = string.Empty;
    public string Apellido_Materno { get; init; } = string.Empty;
    public string Nombre_Completo { get; init; } = string.Empty;
    public string? Licencia_Prof { get; init; }
    public string? SucursalNombre { get; init; }
}
