namespace SOFIA.Domain.Entities;

public sealed class RolSucursal
{
    private RolSucursal() { } // Required for EF Core

    public Guid RolId { get; private set; }
    public Guid SucursalId { get; private set; }

    // Navigation Properties
    public Rol? Rol { get; }
    public Sucursal? Sucursal { get; }

    public static RolSucursal Create(Guid rolId, Guid sucursalId) => new()
    {
        RolId = rolId,
        SucursalId = sucursalId
    };
}
