using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class Cuenta : BaseEntity
{
    private Cuenta() { } // Required for EF Core

    public Guid EmpleadoId { get; private set; }
    public string NombreUsuario { get; private set; } = string.Empty;
    public string PasswordHash { get; private set; } = string.Empty;
    public Guid SecurityStamp { get; private set; }
    public bool RequiereCambioClave { get; private set; }
    public int IntentosFallidos { get; private set; }
    public DateTimeOffset? BloqueadoHasta { get; private set; }
    public bool CuentaActiva { get; private set; }
    public string? RecoveryToken { get; private set; }
    public DateTimeOffset? RecoveryTokenExpiry { get; private set; }

    // Navigation Properties
    public Empleado? Empleado { get; private set; }
    public ICollection<Rol> Roles { get; private set; } = [];
    public ICollection<CuentaRol> CuentasRoles { get; private set; } = [];

    public static Result<Cuenta> Create(
        Guid empleadoId,
        string nombreUsuario,
        string passwordHash,
        Guid? tenantId = null,
        bool requiereCambioClave = true)
    {
        if (empleadoId == Guid.Empty)
        {
            return Result.Failure<Cuenta>(Error.Validation("Cuenta.EmpleadoId", "Empleado ID is required."));
        }

        if (string.IsNullOrWhiteSpace(nombreUsuario))
        {
            return Result.Failure<Cuenta>(Error.Validation("Cuenta.Usuario", "Nombre de Usuario is required."));
        }

        if (string.IsNullOrWhiteSpace(passwordHash))
        {
            return Result.Failure<Cuenta>(Error.Validation("Cuenta.Password", "Password Hash is required."));
        }

        return Result.Success(new Cuenta
        {
            EmpleadoId = empleadoId,
            NombreUsuario = nombreUsuario,
            PasswordHash = passwordHash,
            SecurityStamp = Guid.NewGuid(),
            RequiereCambioClave = requiereCambioClave,
            IntentosFallidos = 0,
            CuentaActiva = true,
            TenantId = tenantId
        });
    }

    public void UpdatePassword(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        SecurityStamp = Guid.NewGuid();
        RequiereCambioClave = false;
    }

    public void RegisterFailedAttempt()
    {
        IntentosFallidos++;
        if (IntentosFallidos >= 5)
        {
            BloqueadoHasta = DateTimeOffset.UtcNow.AddMinutes(15);
        }
    }

    public void ResetFailedAttempts()
    {
        IntentosFallidos = 0;
        BloqueadoHasta = null;
    }

    public void ToggleActive() => CuentaActiva = !CuentaActiva;

    public void InvalidateSecurityStamp() => SecurityStamp = Guid.NewGuid();

    public void GenerateRecoveryToken()
    {
        RecoveryToken = Guid.NewGuid().ToString("N");
        RecoveryTokenExpiry = DateTimeOffset.UtcNow.AddHours(1);
    }

    public Result ResetPassword(string token, string newPasswordHash)
    {
        if (RecoveryToken != token || RecoveryTokenExpiry < DateTimeOffset.UtcNow)
        {
            return Result.Failure(Error.Validation("Auth.InvalidToken", "El token de recuperación es inválido o ha expirado."));
        }

        PasswordHash = newPasswordHash;
        SecurityStamp = Guid.NewGuid();
        RecoveryToken = null;
        RecoveryTokenExpiry = null;
        IntentosFallidos = 0;
        BloqueadoHasta = null;

        return Result.Success();
    }

    public void AddRol(Rol rol)
    {
        if (!Roles.Any(r => r.Id == rol.Id))
        {
            Roles.Add(rol);
            SecurityStamp = Guid.NewGuid();
        }
    }

    public void RemoveRol(Guid rolId)
    {
        var rol = Roles.FirstOrDefault(r => r.Id == rolId);
        if (rol != null)
        {
            _ = Roles.Remove(rol);
            SecurityStamp = Guid.NewGuid();
        }
    }
}
