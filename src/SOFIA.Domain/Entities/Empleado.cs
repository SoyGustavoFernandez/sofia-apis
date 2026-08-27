using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public class Empleado : BaseEntity
{
    private Empleado() { } // Required for EF Core

    public Guid Sucursal_Base_ID { get; private set; }
    public virtual Sucursal? Sucursal_Base { get; private set; }

    public string Nombres { get; private set; } = string.Empty;
    public string Apellido_Paterno { get; private set; } = string.Empty;
    public string Apellido_Materno { get; private set; } = string.Empty;
    public string? Licencia_Prof { get; private set; }

    public string Nombre_Completo => $"{Nombres} {Apellido_Paterno} {Apellido_Materno}".Trim();

    // SQL Server BLOB stored as byte[]
    public byte[]? Huella_Biometrica { get; private set; }

    // Set if the employee manages a branch
    public virtual Sucursal? Sucursal_Gerenciada { get; private set; }

    public static Result<Empleado> Create(
        Guid sucursalBaseId,
        string nombres,
        string apellidoPaterno,
        string apellidoMaterno,
        string? licenciaProf = null,
        byte[]? huellaBiometrica = null) => sucursalBaseId == Guid.Empty
            ? Result.Failure<Empleado>(Error.Validation("Empleado.Sucursal", "Sucursal ID is required."))
            : string.IsNullOrWhiteSpace(nombres)
            ? Result.Failure<Empleado>(Error.Validation("Empleado.Nombres", "Nombres is required."))
            : string.IsNullOrWhiteSpace(apellidoPaterno)
            ? Result.Failure<Empleado>(Error.Validation("Empleado.ApellidoPaterno", "Apellido Paterno is required."))
            : string.IsNullOrWhiteSpace(apellidoMaterno)
            ? Result.Failure<Empleado>(Error.Validation("Empleado.ApellidoMaterno", "Apellido Materno is required."))
            : Result.Success(new Empleado
            {
                Sucursal_Base_ID = sucursalBaseId,
                Nombres = nombres,
                Apellido_Paterno = apellidoPaterno,
                Apellido_Materno = apellidoMaterno,
                Licencia_Prof = licenciaProf,
                Huella_Biometrica = huellaBiometrica
            });

    public Result Update(
        Guid sucursalBaseId,
        string nombres,
        string apellidoPaterno,
        string apellidoMaterno,
        string? licenciaProf,
        byte[]? huellaBiometrica)
    {
        if (sucursalBaseId == Guid.Empty)
        {
            return Result.Failure(Error.Validation("Empleado.Sucursal", "Sucursal ID is required."));
        }

        if (string.IsNullOrWhiteSpace(nombres))
        {
            return Result.Failure(Error.Validation("Empleado.Nombres", "Nombres is required."));
        }

        if (string.IsNullOrWhiteSpace(apellidoPaterno))
        {
            return Result.Failure(Error.Validation("Empleado.ApellidoPaterno", "Apellido Paterno is required."));
        }

        if (string.IsNullOrWhiteSpace(apellidoMaterno))
        {
            return Result.Failure(Error.Validation("Empleado.ApellidoMaterno", "Apellido Materno is required."));
        }

        Sucursal_Base_ID = sucursalBaseId;
        Nombres = nombres;
        Apellido_Paterno = apellidoPaterno;
        Apellido_Materno = apellidoMaterno;
        Licencia_Prof = licenciaProf;
        Huella_Biometrica = huellaBiometrica;

        return Result.Success();
    }
}
