using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public class Sucursal : BaseEntity
{
    private Sucursal() { } // Required for EF Core

    public string Nombre { get; private set; } = string.Empty;
    public string Direccion_Fisica { get; private set; } = string.Empty;
    public string Numero_Licencia { get; private set; } = string.Empty;

    // Relación con el Gerente (Empleado)
    public Guid? Gerente_ID { get; private set; }
    public virtual Empleado? Gerente { get; private set; }

    // Colección de empleados base en esta sucursal
    public virtual ICollection<Empleado> Empleados { get; private set; } = [];

    public static Result<Sucursal> Create(
        string nombre,
        string direccionFisica,
        string numeroLicencia,
        Guid? gerenteId = null) => string.IsNullOrWhiteSpace(nombre)
            ? Result.Failure<Sucursal>(Error.Validation("Sucursal.Nombre", "Nombre is required."))
            : string.IsNullOrWhiteSpace(direccionFisica)
            ? Result.Failure<Sucursal>(Error.Validation("Sucursal.Direccion", "Direccion is required."))
            : string.IsNullOrWhiteSpace(numeroLicencia)
            ? Result.Failure<Sucursal>(Error.Validation("Sucursal.Licencia", "Licencia is required."))
            : Result.Success(new Sucursal
            {
                Nombre = nombre,
                Direccion_Fisica = direccionFisica,
                Numero_Licencia = numeroLicencia,
                Gerente_ID = gerenteId
            });

    public Result Update(
        string nombre,
        string direccionFisica,
        string numeroLicencia,
        Guid? gerenteId)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return Result.Failure(Error.Validation("Sucursal.Nombre", "Nombre is required."));
        }

        if (string.IsNullOrWhiteSpace(direccionFisica))
        {
            return Result.Failure(Error.Validation("Sucursal.Direccion", "Direccion is required."));
        }

        if (string.IsNullOrWhiteSpace(numeroLicencia))
        {
            return Result.Failure(Error.Validation("Sucursal.Licencia", "Licencia is required."));
        }

        Nombre = nombre;
        Direccion_Fisica = direccionFisica;
        Numero_Licencia = numeroLicencia;
        Gerente_ID = gerenteId;

        return Result.Success();
    }
}
