using System.Text.RegularExpressions;
using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public partial class Empleado : BaseEntity
{
    public const int EmailMaxLength = 254;

    private Empleado() { } // Required for EF Core

    public Guid Sucursal_Base_ID { get; private set; }
    public virtual Sucursal? Sucursal_Base { get; }

    public string Nombres { get; private set; } = string.Empty;
    public string Apellido_Paterno { get; private set; } = string.Empty;
    public string Apellido_Materno { get; private set; } = string.Empty;
    public string? Licencia_Prof { get; private set; }

    // Destination of password recovery links; accounts without it cannot self-recover
    public string? Email { get; private set; }

    public string Nombre_Completo => $"{Nombres} {Apellido_Paterno} {Apellido_Materno}".Trim();

    // SQL Server BLOB stored as byte[]
    public byte[]? Huella_Biometrica { get; private set; }

    // Set if the employee manages a branch
    public virtual Sucursal? Sucursal_Gerenciada { get; }

    public static Result<Empleado> Create(
        Guid sucursalBaseId,
        string nombres,
        string apellidoPaterno,
        string apellidoMaterno,
        string? licenciaProf = null,
        byte[]? huellaBiometrica = null,
        Guid? tenantId = null,
        string? email = null)
    {
        if (sucursalBaseId == Guid.Empty)
        {
            return Result.Failure<Empleado>(Error.Validation("Empleado.Sucursal", "Sucursal ID is required."));
        }

        if (string.IsNullOrWhiteSpace(nombres))
        {
            return Result.Failure<Empleado>(Error.Validation("Empleado.Nombres", "Nombres is required."));
        }

        if (string.IsNullOrWhiteSpace(apellidoPaterno))
        {
            return Result.Failure<Empleado>(Error.Validation("Empleado.ApellidoPaterno", "Apellido Paterno is required."));
        }

        if (string.IsNullOrWhiteSpace(apellidoMaterno))
        {
            return Result.Failure<Empleado>(Error.Validation("Empleado.ApellidoMaterno", "Apellido Materno is required."));
        }

        var normalizedEmail = NormalizeEmail(email);
        if (!IsValidEmail(normalizedEmail))
        {
            return Result.Failure<Empleado>(InvalidEmailError);
        }

        return Result.Success(new Empleado
        {
            Sucursal_Base_ID = sucursalBaseId,
            Nombres = nombres,
            Apellido_Paterno = apellidoPaterno,
            Apellido_Materno = apellidoMaterno,
            Licencia_Prof = licenciaProf,
            Huella_Biometrica = huellaBiometrica,
            Email = normalizedEmail,
            TenantId = tenantId
        });
    }

    public Result Update(
        Guid sucursalBaseId,
        string nombres,
        string apellidoPaterno,
        string apellidoMaterno,
        string? licenciaProf,
        byte[]? huellaBiometrica,
        string? email)
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

        var normalizedEmail = NormalizeEmail(email);
        if (!IsValidEmail(normalizedEmail))
        {
            return Result.Failure(InvalidEmailError);
        }

        Sucursal_Base_ID = sucursalBaseId;
        Nombres = nombres;
        Apellido_Paterno = apellidoPaterno;
        Apellido_Materno = apellidoMaterno;
        Licencia_Prof = licenciaProf;
        Huella_Biometrica = huellaBiometrica;
        Email = normalizedEmail;

        return Result.Success();
    }

    // Blank means no email; stored trimmed and lower-cased so the per-company uniqueness is case-insensitive
    public static string? NormalizeEmail(string? email) =>
        string.IsNullOrWhiteSpace(email) ? null : email.Trim().ToLowerInvariant();

    private static readonly Error InvalidEmailError =
        Error.Validation("Empleado.Email.Invalido", "Email is not a valid address.");

    private static bool IsValidEmail(string? normalizedEmail) =>
        normalizedEmail is null || (normalizedEmail.Length <= EmailMaxLength && EmailRegex().IsMatch(normalizedEmail));

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
