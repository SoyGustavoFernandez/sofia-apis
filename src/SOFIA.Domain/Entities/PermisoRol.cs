using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class PermisoRol : BaseEntity
{
    private PermisoRol() { } // Required for EF Core

    public Guid RolId { get; private set; }
    public string ModuloSistema { get; private set; } = string.Empty;
    public string Accion { get; private set; } = string.Empty;

    // Navigation Properties
    public Rol? Rol { get; }

    public static Result<PermisoRol> Create(
        Guid rolId,
        string moduloSistema,
        string accion)
    {
        if (rolId == Guid.Empty)
        {
            return Result.Failure<PermisoRol>(Error.Validation("Permiso.RolId", "Rol ID is required."));
        }

        if (string.IsNullOrWhiteSpace(moduloSistema))
        {
            return Result.Failure<PermisoRol>(Error.Validation("Permiso.Modulo", "Modulo Sistema is required."));
        }

        if (string.IsNullOrWhiteSpace(accion))
        {
            return Result.Failure<PermisoRol>(Error.Validation("Permiso.Accion", "Accion is required."));
        }

        return Result.Success(new PermisoRol
        {
            RolId = rolId,
            ModuloSistema = moduloSistema,
            Accion = accion
        });
    }
}
