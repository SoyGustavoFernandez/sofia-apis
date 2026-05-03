namespace SOFIA.Application.Sucursales;

public class SucursalDto
{
    public Guid Id { get; init; }
    public string Nombre { get; init; } = string.Empty;
    public string DireccionFisica { get; init; } = string.Empty;
    public string NumeroLicencia { get; init; } = string.Empty;
    public Guid? GerenteId { get; init; }
    public string? GerenteNombre { get; init; }
}
