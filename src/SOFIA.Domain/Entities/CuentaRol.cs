namespace SOFIA.Domain.Entities;

public sealed class CuentaRol
{
    private CuentaRol() { } // Required for EF Core

    public Guid CuentaId { get; private set; }
    public Guid RolId { get; private set; }
    public DateTimeOffset AssignedAt { get; private set; } = DateTimeOffset.UtcNow;
    public string? AssignedBy { get; private set; }

    // Navigation Properties
    public Cuenta? Cuenta { get; }
    public Rol? Rol { get; }

    public static CuentaRol Create(Guid cuentaId, Guid rolId, string? assignedBy = null) => new()
    {
        CuentaId = cuentaId,
        RolId = rolId,
        AssignedAt = DateTimeOffset.UtcNow,
        AssignedBy = assignedBy
    };
}
