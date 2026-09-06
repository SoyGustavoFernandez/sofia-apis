namespace SOFIA.Domain.Entities;

public sealed class CuentaSucursal
{
    private CuentaSucursal() { } // Required for EF Core

    public Guid CuentaId { get; private set; }
    public Guid SucursalId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; } = DateTimeOffset.UtcNow;
    public string? AssignedBy { get; private set; }

    // Navigation Properties
    public Cuenta? Cuenta { get; }
    public Sucursal? Sucursal { get; }

    public static CuentaSucursal Create(Guid cuentaId, Guid sucursalId, string? assignedBy = null) => new()
    {
        CuentaId = cuentaId,
        SucursalId = sucursalId,
        AssignedAt = DateTimeOffset.UtcNow,
        AssignedBy = assignedBy
    };
}
