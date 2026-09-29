using System.Security.Cryptography;
using System.Text;
using SOFIA.Domain.Common;

namespace SOFIA.Domain.Entities;

public sealed class Cuenta : BaseEntity
{
    // Shared with the reset handler so an unknown username is indistinguishable from a bad token
    public static readonly Error InvalidRecoveryTokenError =
        Error.Validation("Auth.InvalidToken", "The recovery token is invalid or has expired.");

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
    public Empleado? Empleado { get; }
    public ICollection<Rol> Roles { get; private set; } = [];
    public ICollection<CuentaRol> CuentasRoles { get; private set; } = [];
    public ICollection<Sucursal> Sucursales { get; private set; } = [];
    public ICollection<CuentaSucursal> CuentasSucursales { get; private set; } = [];

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
        RecoveryToken = null;
        RecoveryTokenExpiry = null;
    }

    public void RegisterFailedAttempt()
    {
        // An expired lock starts a fresh count, otherwise one failure every 15 minutes keeps a known username locked
        if (BloqueadoHasta <= DateTimeOffset.UtcNow)
        {
            ResetFailedAttempts();
        }

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

    public void ForcePasswordChange() => RequiereCambioClave = true;

    // Only the token's hash is stored, so a database leak does not expose usable recovery tokens
    public void GenerateRecoveryToken(string tokenHash)
    {
        RecoveryToken = tokenHash;
        RecoveryTokenExpiry = DateTimeOffset.UtcNow.AddHours(1);
    }

    public Result ResetPassword(string tokenHash, string newPasswordHash)
    {
        if (RecoveryToken is null ||
            !(RecoveryTokenExpiry >= DateTimeOffset.UtcNow) ||
            !CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(RecoveryToken), Encoding.UTF8.GetBytes(tokenHash)))
        {
            return Result.Failure(InvalidRecoveryTokenError);
        }

        PasswordHash = newPasswordHash;
        SecurityStamp = Guid.NewGuid();
        RequiereCambioClave = false;
        RecoveryToken = null;
        RecoveryTokenExpiry = null;
        IntentosFallidos = 0;
        BloqueadoHasta = null;

        return Result.Success();
    }

    public bool EsAdmin => Roles.Any(r => r.EsAdmin);

    // Requires Empleado.Sucursal_Base loaded; a base branch of another company must never yield a token
    public bool SucursalBasePerteneceAlTenant =>
        TenantId is not null && Empleado?.Sucursal_Base?.TenantId == TenantId;

    public void AddRol(Rol rol)
    {
        if (!Roles.Any(r => r.Id == rol.Id))
        {
            Roles.Add(rol);
        }
    }

    public void RemoveRol(Guid rolId)
    {
        var rol = Roles.FirstOrDefault(r => r.Id == rolId);
        if (rol != null)
        {
            _ = Roles.Remove(rol);
        }
    }

    public void AddSucursal(Sucursal sucursal)
    {
        if (!Sucursales.Any(s => s.Id == sucursal.Id))
        {
            Sucursales.Add(sucursal);
        }
    }

    public void RemoveSucursal(Guid sucursalId)
    {
        var sucursal = Sucursales.FirstOrDefault(s => s.Id == sucursalId);
        if (sucursal != null)
        {
            _ = Sucursales.Remove(sucursal);
        }
    }
}
