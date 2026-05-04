using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class PermisoRol : BaseEntity
{
    private PermisoRol() { } // Required for EF Core

    public Guid RolId { get; private set; }
    public string ModuloSistema { get; private set; } = string.Empty;
    public string Accion { get; private set; } = string.Empty;

    // Navigation Properties
    public Rol? Rol { get; private set; }

    public static Result<PermisoRol> Create(
        Guid rolId,
        string moduloSistema,
        string accion) =>
        rolId == Guid.Empty
            ? Result.Failure<PermisoRol>(Error.Validation("Permiso.RolId", "Rol ID is required."))
            : string.IsNullOrWhiteSpace(moduloSistema)
            ? Result.Failure<PermisoRol>(Error.Validation("Permiso.Modulo", "Modulo Sistema is required."))
            : string.IsNullOrWhiteSpace(accion)
            ? Result.Failure<PermisoRol>(Error.Validation("Permiso.Accion", "Accion is required."))
            : Result.Success(new PermisoRol
            {
                RolId = rolId,
                ModuloSistema = moduloSistema,
                Accion = accion
            });
}
