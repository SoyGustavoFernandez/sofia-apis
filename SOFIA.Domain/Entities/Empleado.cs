using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public class Empleado : BaseEntity
{
    private Empleado() { } // Required for EF Core

    public Guid Sucursal_Base_ID { get; private set; }
    public virtual Sucursal? Sucursal_Base { get; private set; }

    public string Nombre_Completo { get; private set; } = string.Empty;
    public string Rol_Sistema { get; private set; } = string.Empty;
    public string? Licencia_Prof { get; private set; }

    // BLOB en SQL Server -> byte[]
    public byte[]? Huella_Biometrica { get; private set; }

    // Si es gerente de una sucursal
    public virtual Sucursal? Sucursal_Gerenciada { get; private set; }

    public static Result<Empleado> Create(
        Guid sucursalBaseId,
        string nombreCompleto,
        string rolSistema,
        string? licenciaProf = null,
        byte[]? huellaBiometrica = null) => sucursalBaseId == Guid.Empty
            ? Result.Failure<Empleado>(Error.Validation("Empleado.Sucursal", "Sucursal ID is required."))
            : string.IsNullOrWhiteSpace(nombreCompleto)
            ? Result.Failure<Empleado>(Error.Validation("Empleado.Nombre", "Nombre Completo is required."))
            : string.IsNullOrWhiteSpace(rolSistema)
            ? Result.Failure<Empleado>(Error.Validation("Empleado.Rol", "Rol Sistema is required."))
            : Result.Success(new Empleado
            {
                Sucursal_Base_ID = sucursalBaseId,
                Nombre_Completo = nombreCompleto,
                Rol_Sistema = rolSistema,
                Licencia_Prof = licenciaProf,
                Huella_Biometrica = huellaBiometrica
            });

    public Result Update(
        Guid sucursalBaseId,
        string nombreCompleto,
        string rolSistema,
        string? licenciaProf,
        byte[]? huellaBiometrica)
    {
        if (sucursalBaseId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Empleado.Sucursal", "Sucursal ID is required."));
        }

        if (string.IsNullOrWhiteSpace(nombreCompleto))
        {
            return Result.Failure(Error.Validation("Empleado.Nombre", "Nombre Completo is required."));
        }

        if (string.IsNullOrWhiteSpace(rolSistema))
        {
            return Result.Failure(Error.Validation("Empleado.Rol", "Rol Sistema is required."));
        }

        Sucursal_Base_ID = sucursalBaseId;
        Nombre_Completo = nombreCompleto;
        Rol_Sistema = rolSistema;
        Licencia_Prof = licenciaProf;
        Huella_Biometrica = huellaBiometrica;

        return Result.Success();
    }
}
